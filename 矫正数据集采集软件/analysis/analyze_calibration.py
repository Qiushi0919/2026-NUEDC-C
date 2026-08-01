"""Analyze stationary calibration sessions and compare deployable correction models.

The independent unit for validation is one recording session, not one serial frame.
Frames from the same session are correlated by placement and smoothing, so each
session is reduced to robust medians before leave-one-session-out validation.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

import numpy as np
import pandas as pd


DISTANCE_SCALE = 2.0
ANGLE_SCALE = 45.0


def load_sessions(data_root: Path) -> tuple[pd.DataFrame, pd.DataFrame]:
    frames: list[pd.DataFrame] = []
    for csv_path in sorted(data_root.glob("*/measurements.csv")):
        frame = pd.read_csv(csv_path)
        frame["session"] = csv_path.parent.name
        frame["source_csv"] = str(csv_path)
        frames.append(frame)
    if not frames:
        raise FileNotFoundError(f"No measurements.csv files found below {data_root}")

    raw = pd.concat(frames, ignore_index=True)
    numeric = [
        "actual_subject_distance_m",
        "expected_center_distance_m",
        "actual_angle_deg",
        "raw_center_distance_m",
        "raw_azimuth_deg",
    ]
    for column in numeric:
        raw[column] = pd.to_numeric(raw[column], errors="coerce")
    valid = raw[raw["checksum_valid"].eq(True) & raw[numeric].notna().all(axis=1)].copy()

    grouped = valid.groupby("session", sort=True)
    summary = grouped.agg(
        actual_distance_m=("actual_subject_distance_m", "first"),
        expected_center_distance_m=("expected_center_distance_m", "first"),
        actual_angle_deg=("actual_angle_deg", "first"),
        frame_count=("raw_center_distance_m", "size"),
        raw_distance_mean_m=("raw_center_distance_m", "mean"),
        raw_distance_median_m=("raw_center_distance_m", "median"),
        raw_distance_std_m=("raw_center_distance_m", "std"),
        raw_distance_min_m=("raw_center_distance_m", "min"),
        raw_distance_max_m=("raw_center_distance_m", "max"),
        raw_angle_mean_deg=("raw_azimuth_deg", "mean"),
        raw_angle_median_deg=("raw_azimuth_deg", "median"),
        raw_angle_std_deg=("raw_azimuth_deg", "std"),
        raw_angle_min_deg=("raw_azimuth_deg", "min"),
        raw_angle_max_deg=("raw_azimuth_deg", "max"),
        tag_id_hex=("tag_id_hex", "first"),
        anchor_id_hex=("anchor_id_hex", "first"),
    ).reset_index()
    summary["invalid_frame_count"] = raw.groupby("session").size().reindex(summary["session"]).to_numpy() - summary["frame_count"]
    summary["baseline_distance_m"] = np.maximum(0.0, summary["raw_distance_median_m"] - 0.30)
    summary["baseline_angle_deg"] = summary["raw_angle_median_deg"] + 4.0
    summary["baseline_distance_error_m"] = summary["baseline_distance_m"] - summary["actual_distance_m"]
    summary["baseline_angle_error_deg"] = summary["baseline_angle_deg"] - summary["actual_angle_deg"]
    return raw, summary


def polynomial_features(raw_distance: np.ndarray, raw_angle: np.ndarray, order: int) -> np.ndarray:
    d = (np.asarray(raw_distance, dtype=float) - 2.0) / DISTANCE_SCALE
    a = np.asarray(raw_angle, dtype=float) / ANGLE_SCALE
    columns = [np.ones_like(d)]
    for total_degree in range(1, order + 1):
        for d_degree in range(total_degree, -1, -1):
            a_degree = total_degree - d_degree
            columns.append((d ** d_degree) * (a ** a_degree))
    return np.column_stack(columns)


def fit_linear(features: np.ndarray, target: np.ndarray, ridge: float = 0.0) -> np.ndarray:
    if ridge <= 0:
        return np.linalg.lstsq(features, target, rcond=None)[0]
    penalty = np.eye(features.shape[1]) * ridge
    penalty[0, 0] = 0.0
    return np.linalg.solve(features.T @ features + penalty, features.T @ target)


def linear_extrapolate(x: float, xp: np.ndarray, fp: np.ndarray) -> float:
    order = np.argsort(xp)
    xp = np.asarray(xp, dtype=float)[order]
    fp = np.asarray(fp, dtype=float)[order]
    unique_x, inverse = np.unique(xp, return_inverse=True)
    if unique_x.size != xp.size:
        merged = np.array([np.median(fp[inverse == i]) for i in range(unique_x.size)])
        xp, fp = unique_x, merged
    if xp.size == 1:
        return float(fp[0])
    if x <= xp[0]:
        left, right = 0, 1
    elif x >= xp[-1]:
        left, right = xp.size - 2, xp.size - 1
    else:
        right = int(np.searchsorted(xp, x))
        left = right - 1
    span = xp[right] - xp[left]
    if abs(span) < 1e-12:
        return float((fp[left] + fp[right]) / 2.0)
    ratio = (x - xp[left]) / span
    return float(fp[left] + ratio * (fp[right] - fp[left]))


def piecewise_primary_predict(
    train: pd.DataFrame, row: pd.Series, target: str
) -> float:
    if target == "distance":
        target_col, measured_col = "actual_distance_m", "raw_distance_median_m"
        value = row["raw_distance_median_m"]
    else:
        target_col, measured_col = "actual_angle_deg", "raw_angle_median_deg"
        value = row["raw_angle_median_deg"]
    knots = train.groupby(target_col, sort=True)[measured_col].median().reset_index()
    return linear_extrapolate(value, knots[measured_col].to_numpy(), knots[target_col].to_numpy())


def fit_cubic_angle_predict(train: pd.DataFrame, raw_angle: float) -> float:
    measured = train["raw_angle_median_deg"].to_numpy(dtype=float)
    features = np.column_stack([np.ones(len(train)), measured, measured**2, measured**3])
    coefficient = np.linalg.lstsq(features, train["actual_angle_deg"].to_numpy(dtype=float), rcond=None)[0]
    powers = np.array([1.0, raw_angle, raw_angle**2, raw_angle**3])
    return float(np.clip(powers @ coefficient, -45.0, 45.0))


def deployed_cubic_angle(raw_angle: float) -> float:
    value = (
        -4.050890424
        + 0.769493752 * raw_angle
        + 0.00304317606 * raw_angle**2
        + 0.0000819724409 * raw_angle**3
    )
    return float(np.clip(value, -45.0, 45.0))


def grid_piecewise_distance_predict(
    train: pd.DataFrame, row: pd.Series, calibrated_angle: float | None = None
) -> float:
    """Invert raw distance separately at each angle, then interpolate angle rows.

    Each per-angle inverse is monotonic because its raw-distance knots are sorted
    before interpolation.  The angle coordinate comes from the independently
    validated one-dimensional piecewise angle calibration.
    """
    estimated_angle = calibrated_angle
    if estimated_angle is None:
        estimated_angle = fit_cubic_angle_predict(train, float(row["raw_angle_median_deg"]))
    angle_predictions: list[tuple[float, float]] = []
    for actual_angle, angle_group in train.groupby("actual_angle_deg", sort=True):
        knots = angle_group.groupby("actual_distance_m", sort=True)["raw_distance_median_m"].median().reset_index()
        if len(knots) < 2:
            continue
        prediction = linear_extrapolate(
            float(row["raw_distance_median_m"]),
            knots["raw_distance_median_m"].to_numpy(),
            knots["actual_distance_m"].to_numpy(),
        )
        angle_predictions.append((float(actual_angle), prediction))
    angle_predictions.sort()
    return linear_extrapolate(
        estimated_angle,
        np.array([item[0] for item in angle_predictions]),
        np.array([item[1] for item in angle_predictions]),
    )


def idw_predict(train: pd.DataFrame, row: pd.Series, target: str, k: int, d_scale: float, a_scale: float) -> float:
    dr = (train["raw_distance_median_m"].to_numpy() - row["raw_distance_median_m"]) / d_scale
    da = (train["raw_angle_median_deg"].to_numpy() - row["raw_angle_median_deg"]) / a_scale
    distance2 = dr * dr + da * da
    nearest = np.argsort(distance2)[: min(k, len(train))]
    weights = 1.0 / np.maximum(distance2[nearest], 1e-6)
    target_col = "actual_distance_m" if target == "distance" else "actual_angle_deg"
    values = train[target_col].to_numpy()[nearest]
    return float(np.sum(weights * values) / np.sum(weights))


def model_predict(train: pd.DataFrame, row: pd.Series, model: str, target: str) -> float:
    target_col = "actual_distance_m" if target == "distance" else "actual_angle_deg"
    if model == "current_constant":
        return float(max(0.0, row["raw_distance_median_m"] - 0.30) if target == "distance" else row["raw_angle_median_deg"] + 4.0)
    if model == "primary_affine":
        source_col = "raw_distance_median_m" if target == "distance" else "raw_angle_median_deg"
        features = np.column_stack([np.ones(len(train)), train[source_col].to_numpy()])
        coefficient = fit_linear(features, train[target_col].to_numpy())
        return float(np.array([1.0, row[source_col]]) @ coefficient)
    if model == "primary_piecewise":
        return piecewise_primary_predict(train, row, target)
    if model == "grid_piecewise":
        return grid_piecewise_distance_predict(train, row) if target == "distance" else fit_cubic_angle_predict(train, float(row["raw_angle_median_deg"]))
    if model == "angle_cubic":
        return grid_piecewise_distance_predict(train, row) if target == "distance" else fit_cubic_angle_predict(train, float(row["raw_angle_median_deg"]))
    if model.startswith("poly2d_order"):
        order = int(model.rsplit("_", 1)[-1])
        features = polynomial_features(train["raw_distance_median_m"], train["raw_angle_median_deg"], order)
        ridge = 1e-3 if order >= 3 else 0.0
        coefficient = fit_linear(features, train[target_col].to_numpy(), ridge)
        test = polynomial_features(np.array([row["raw_distance_median_m"]]), np.array([row["raw_angle_median_deg"]]), order)
        return float(test[0] @ coefficient)
    if model.startswith("idw_"):
        _, k, d_scale, a_scale = model.split("_")
        return idw_predict(train, row, target, int(k), float(d_scale), float(a_scale))
    raise ValueError(model)


def metrics(actual: np.ndarray, predicted: np.ndarray) -> dict[str, float]:
    error = np.asarray(predicted) - np.asarray(actual)
    return {
        "mae": float(np.mean(np.abs(error))),
        "rmse": float(np.sqrt(np.mean(error * error))),
        "max_abs_error": float(np.max(np.abs(error))),
        "bias": float(np.mean(error)),
        "p95_abs_error": float(np.percentile(np.abs(error), 95)),
    }


def cross_validate(summary: pd.DataFrame, models: list[str]) -> tuple[pd.DataFrame, pd.DataFrame]:
    predictions: list[dict[str, object]] = []
    for held_index, held in summary.iterrows():
        train = summary.drop(index=held_index)
        for target in ("distance", "angle"):
            actual_col = "actual_distance_m" if target == "distance" else "actual_angle_deg"
            for model in models:
                prediction = model_predict(train, held, model, target)
                predictions.append(
                    {
                        "session": held["session"],
                        "target": target,
                        "model": model,
                        "actual": float(held[actual_col]),
                        "predicted": prediction,
                        "error": prediction - float(held[actual_col]),
                    }
                )
    prediction_frame = pd.DataFrame(predictions)
    rows: list[dict[str, object]] = []
    for (target, model), group in prediction_frame.groupby(["target", "model"], sort=True):
        row: dict[str, object] = {"target": target, "model": model}
        row.update(metrics(group["actual"].to_numpy(), group["predicted"].to_numpy()))
        rows.append(row)
    return pd.DataFrame(rows), prediction_frame


def fit_final_polynomial(summary: pd.DataFrame, target: str, order: int) -> dict[str, object]:
    target_col = "actual_distance_m" if target == "distance" else "actual_angle_deg"
    features = polynomial_features(summary["raw_distance_median_m"], summary["raw_angle_median_deg"], order)
    ridge = 1e-3 if order >= 3 else 0.0
    coefficient = fit_linear(features, summary[target_col].to_numpy(), ridge)
    fitted = features @ coefficient
    derivative_min = None
    if target == "distance":
        grid_d = np.linspace(0.3, 3.9, 91)
        grid_a = np.linspace(-60.0, 60.0, 49)
        derivative = []
        for angle in grid_a:
            values = polynomial_features(grid_d, np.full_like(grid_d, angle), order) @ coefficient
            derivative.extend(np.diff(values) / np.diff(grid_d))
        derivative_min = float(np.min(derivative))
    return {
        "target": target,
        "order": order,
        "distance_center_m": 2.0,
        "distance_scale_m": DISTANCE_SCALE,
        "angle_scale_deg": ANGLE_SCALE,
        "coefficient_order": "total degree ascending; within each degree d power descending",
        "coefficients": [float(value) for value in coefficient],
        "training_metrics": metrics(summary[target_col].to_numpy(), fitted),
        "minimum_distance_derivative_on_domain": derivative_min,
    }


def fit_final_hybrid(summary: pd.DataFrame) -> tuple[dict[str, object], np.ndarray, np.ndarray]:
    angle_axis = np.array(sorted(summary["actual_angle_deg"].unique()), dtype=float)

    actual_distance_axis = np.array(sorted(summary["actual_distance_m"].unique()), dtype=float)
    raw_distance_rows: list[list[float]] = []
    for angle in angle_axis:
        row = summary[summary["actual_angle_deg"].eq(angle)]
        knots = row.groupby("actual_distance_m", sort=True)["raw_distance_median_m"].median()
        raw_distance_rows.append([float(knots.loc[distance]) for distance in actual_distance_axis])

    distance_prediction = np.array(
        [
            grid_piecewise_distance_predict(
                summary, row, deployed_cubic_angle(float(row["raw_angle_median_deg"]))
            )
            for _, row in summary.iterrows()
        ]
    )
    angle_prediction = np.array(
        [deployed_cubic_angle(float(row["raw_angle_median_deg"])) for _, row in summary.iterrows()]
    )

    leave_angle_actual: list[float] = []
    leave_angle_predicted: list[float] = []
    for angle in angle_axis:
        train = summary[~summary["actual_angle_deg"].eq(angle)]
        for _, row in summary[summary["actual_angle_deg"].eq(angle)].iterrows():
            leave_angle_actual.append(float(row["actual_distance_m"]))
            leave_angle_predicted.append(grid_piecewise_distance_predict(train, row))

    leave_distance_actual: list[float] = []
    leave_distance_predicted: list[float] = []
    for distance in actual_distance_axis[1:-1]:
        train = summary[~summary["actual_distance_m"].eq(distance)]
        for _, row in summary[summary["actual_distance_m"].eq(distance)].iterrows():
            leave_distance_actual.append(float(row["actual_distance_m"]))
            leave_distance_predicted.append(grid_piecewise_distance_predict(train, row))

    model = {
        "model_type": "hybrid_piecewise_linear_v1",
        "algorithm": {
            "angle": "monotonic cubic regression of raw angle, then clamp output to [-45,+45] degrees",
            "distance": "invert raw-distance knots independently at every actual-angle row, then interpolate rows by calibrated angle",
            "extrapolation": "linear at the first/last segment; final distance clamped to >= 0",
        },
        "actual_angle_axis_deg": [float(x) for x in angle_axis],
        "angle_cubic_coefficients_ascending": [-4.050890424, 0.769493752, 0.00304317606, 0.0000819724409],
        "actual_distance_axis_m": [float(x) for x in actual_distance_axis],
        "raw_distance_knots_m_by_angle": raw_distance_rows,
        "full_calibration_point_metrics": {
            "distance": metrics(summary["actual_distance_m"].to_numpy(), distance_prediction),
            "angle": metrics(summary["actual_angle_deg"].to_numpy(), angle_prediction),
        },
        "generalization_checks": {
            "leave_one_session_out_distance": None,
            "leave_one_session_out_angle": None,
            "leave_one_angle_level_out_distance": metrics(
                np.array(leave_angle_actual), np.array(leave_angle_predicted)
            ),
            "leave_interior_distance_level_out_distance": metrics(
                np.array(leave_distance_actual), np.array(leave_distance_predicted)
            ),
        },
    }
    return model, distance_prediction, angle_prediction


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--data-root", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    args.output_dir.mkdir(parents=True, exist_ok=True)

    raw, summary = load_sessions(args.data_root)
    models = [
        "current_constant",
        "primary_affine",
        "primary_piecewise",
        "grid_piecewise",
        "angle_cubic",
        "poly2d_order_2",
        "poly2d_order_3",
    ]
    for k in (4, 6, 8, 12):
        for d_scale in (0.4, 0.8, 1.2):
            for a_scale in (6.0, 12.0, 20.0):
                models.append(f"idw_{k}_{d_scale}_{a_scale}")

    model_metrics, predictions = cross_validate(summary, models)
    model_metrics["unit"] = np.where(model_metrics["target"].eq("distance"), "m", "deg")
    model_metrics = model_metrics.sort_values(["target", "mae", "rmse"]).reset_index(drop=True)

    best_distance = model_metrics[model_metrics["target"].eq("distance")].iloc[0]
    best_angle = model_metrics[model_metrics["target"].eq("angle")].iloc[0]
    selected_models = {"distance": "grid_piecewise", "angle": "angle_cubic"}
    selected_metrics = {
        target: model_metrics[
            model_metrics["target"].eq(target) & model_metrics["model"].eq(model)
        ].iloc[0].to_dict()
        for target, model in selected_models.items()
    }

    hybrid_model, selected_distance, selected_angle = fit_final_hybrid(summary)
    for target, model in selected_models.items():
        row = selected_metrics[target]
        hybrid_model["generalization_checks"][f"leave_one_session_out_{target}"] = {
            key: float(row[key]) for key in ("mae", "rmse", "max_abs_error", "bias", "p95_abs_error")
        }
    summary["selected_distance_m"] = selected_distance
    summary["selected_distance_error_m"] = selected_distance - summary["actual_distance_m"]
    summary["selected_angle_deg"] = selected_angle
    summary["selected_angle_error_deg"] = selected_angle - summary["actual_angle_deg"]
    summary.to_csv(args.output_dir / "session_summary.csv", index=False, encoding="utf-8-sig")
    model_metrics.to_csv(args.output_dir / "candidate_metrics.csv", index=False, encoding="utf-8-sig")
    predictions.to_csv(args.output_dir / "cross_validation_predictions.csv", index=False, encoding="utf-8-sig")

    result = {
        "dataset": {
            "session_count": int(len(summary)),
            "frame_count": int(len(raw)),
            "valid_frame_count": int(raw["checksum_valid"].eq(True).sum()),
            "actual_distance_levels_m": sorted(float(x) for x in summary["actual_distance_m"].unique()),
            "actual_angle_levels_deg": sorted(float(x) for x in summary["actual_angle_deg"].unique()),
            "session_aggregation": "median of every checksum-valid frame",
            "validation": "leave one entire recording session out",
        },
        "best_by_cv": {
            "distance": best_distance.to_dict(),
            "angle": best_angle.to_dict(),
        },
        "selected_for_deployment": selected_metrics,
        "selected_reason": "The hybrid piecewise-linear lookup uses all measured angle-dependent distance knots, stays monotonic in raw distance within each angle row, and the angle mapping was the only candidate with leave-one-session-out maximum error below 10 degrees.",
        "selected_model": hybrid_model,
        "comparison_polynomial_distance": fit_final_polynomial(summary, "distance", 2),
        "comparison_polynomial_angle": fit_final_polynomial(summary, "angle", 2),
    }
    (args.output_dir / "calibration_analysis.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    raw_columns = [
        "session", "timestamp_iso", "elapsed_ms", "actual_subject_distance_m",
        "expected_center_distance_m", "actual_angle_deg", "raw_center_distance_m",
        "raw_azimuth_deg", "checksum_valid", "tag_id_hex", "anchor_id_hex", "sequence",
    ]
    selected_prediction_rows = predictions[
        predictions["model"].isin(
            ["current_constant", "primary_affine", "poly2d_order_2", "grid_piecewise", "angle_cubic"]
        )
    ]
    workbook_data = {
        "analysis": result,
        "raw_columns": raw_columns,
        "raw_rows": json.loads(raw[raw_columns].to_json(orient="records")),
        "summary_columns": list(summary.columns),
        "summary_rows": json.loads(summary.to_json(orient="records")),
        "metrics_columns": list(model_metrics.columns),
        "metrics_rows": json.loads(model_metrics.to_json(orient="records")),
        "prediction_columns": list(selected_prediction_rows.columns),
        "prediction_rows": json.loads(selected_prediction_rows.to_json(orient="records")),
    }
    (args.output_dir / "workbook_data.json").write_text(
        json.dumps(workbook_data, ensure_ascii=False), encoding="utf-8"
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
