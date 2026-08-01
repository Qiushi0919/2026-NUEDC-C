namespace DigitalKeyLab;

public readonly record struct CalibratedMeasurement(double DistanceM, double AngleDeg);

/// <summary>
/// Field calibration confirmed by on-site testing.
/// Distance preserves the base-station center distance. Angle uses a
/// continuous symmetric piecewise-linear mapping.
/// </summary>
public static class CalibrationModel
{
    public const int Version = 3;
    public const string DisplayName = "场地畸变校准 v3";

    // The collector retains this physical offset for its historical
    // "subject distance" and "center distance" dataset columns.
    public const double DistanceCorrectionM = 0.25;

    public static CalibratedMeasurement Apply(double rawCenterDistanceM, double rawAzimuthDeg)
    {
        Validate(rawCenterDistanceM, rawAzimuthDeg);
        return new CalibratedMeasurement(
            Math.Max(0, rawCenterDistanceM),
            CorrectAngle(rawAzimuthDeg));
    }

    public static CalibratedMeasurement ApplyBaseline(double rawCenterDistanceM, double rawAzimuthDeg)
    {
        Validate(rawCenterDistanceM, rawAzimuthDeg);
        return new CalibratedMeasurement(
            Math.Max(0, rawCenterDistanceM),
            rawAzimuthDeg);
    }

    public static double CorrectAngle(double rawAzimuthDeg)
    {
        if (!double.IsFinite(rawAzimuthDeg))
            throw new ArgumentOutOfRangeException(nameof(rawAzimuthDeg), "Angle input must be finite.");

        if (rawAzimuthDeg < -15)
            return rawAzimuthDeg - 5;
        if (rawAzimuthDeg < -10)
            return 2 * rawAzimuthDeg + 10;
        if (rawAzimuthDeg <= 10)
            return rawAzimuthDeg;
        if (rawAzimuthDeg <= 15)
            return 2 * rawAzimuthDeg - 10;
        return rawAzimuthDeg + 5;
    }

    public static double ClampDisplayAngle(double angleDeg)
    {
        if (!double.IsFinite(angleDeg))
            throw new ArgumentOutOfRangeException(nameof(angleDeg), "Display angle must be finite.");
        return Math.Clamp(angleDeg, -45.0, 45.0);
    }

    private static void Validate(double rawCenterDistanceM, double rawAzimuthDeg)
    {
        if (!double.IsFinite(rawCenterDistanceM) || !double.IsFinite(rawAzimuthDeg))
            throw new ArgumentOutOfRangeException(nameof(rawCenterDistanceM), "Calibration inputs must be finite.");
    }
}
