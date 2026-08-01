using DigitalKeyLab;

namespace CalibrationCollector;

public readonly record struct ProcessedMeasurement(
    double PrefilterDistanceM,
    double PrefilterAngleDeg,
    double CalibratedDistanceM,
    double CalibratedAngleDeg,
    double SmoothedDistanceM,
    double SmoothedAngleDeg,
    int PrefilterSampleCount,
    int SmoothingSampleCount)
{
    public bool SmoothingReady => SmoothingSampleCount >= MeasurementProcessor.OutputWindowSize;
}

/// <summary>
/// Mirrors the deployed main-program pipeline used for the new collection:
/// five-point median prefilter, field piecewise calibration v2, then 30-point median output.
/// </summary>
public sealed class MeasurementProcessor
{
    public const int PrefilterWindowSize = 5;
    public const int OutputWindowSize = 30;

    private readonly Queue<double> _rawDistances = new();
    private readonly Queue<double> _rawAngles = new();
    private readonly MeasurementSmoother _outputSmoother = new();

    public MeasurementProcessor() =>
        _outputSmoother.Configure(OutputWindowSize, SmoothingMethod.Median);

    public ProcessedMeasurement Add(double rawCenterDistanceM, double rawAzimuthDeg)
    {
        Enqueue(_rawDistances, rawCenterDistanceM, PrefilterWindowSize);
        Enqueue(_rawAngles, rawAzimuthDeg, PrefilterWindowSize);
        var prefilterDistance = Median(_rawDistances);
        var prefilterAngle = Median(_rawAngles);
        var calibrated = CalibrationModel.Apply(prefilterDistance, prefilterAngle);
        var smoothed = _outputSmoother.Add(calibrated.DistanceM, calibrated.AngleDeg);
        return new ProcessedMeasurement(
            prefilterDistance,
            prefilterAngle,
            calibrated.DistanceM,
            calibrated.AngleDeg,
            smoothed.DistanceM,
            smoothed.AngleDeg,
            _rawDistances.Count,
            smoothed.SampleCount);
    }

    public void Clear()
    {
        _rawDistances.Clear();
        _rawAngles.Clear();
        _outputSmoother.Clear();
    }

    private static void Enqueue(Queue<double> queue, double value, int capacity)
    {
        queue.Enqueue(value);
        while (queue.Count > capacity)
            queue.Dequeue();
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        return ordered.Length % 2 == 1
            ? ordered[ordered.Length / 2]
            : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2.0;
    }
}
