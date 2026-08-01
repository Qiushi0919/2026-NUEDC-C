namespace DigitalKeyLab;

public enum SmoothingMethod
{
    Mean,
    Median
}

public readonly record struct SmoothedMeasurement(
    double DistanceM,
    double AngleDeg,
    int SampleCount);

public sealed class MeasurementSmoother
{
    public const int DefaultWindowSize = 30;
    public const int MaximumWindowSize = 200;

    private readonly Queue<double> _distanceSamples = new();
    private readonly Queue<double> _angleSamples = new();

    public int WindowSize { get; private set; } = DefaultWindowSize;
    public SmoothingMethod Method { get; private set; } = SmoothingMethod.Mean;
    public int SampleCount => _distanceSamples.Count;

    public static double EstimateDelaySeconds(int windowSize, double sampleRateHz)
    {
        if (windowSize < 1)
            throw new ArgumentOutOfRangeException(nameof(windowSize));
        if (!double.IsFinite(sampleRateHz) || sampleRateHz <= 0)
            return double.NaN;
        return (windowSize - 1) / (2.0 * sampleRateHz);
    }

    public void Configure(int windowSize, SmoothingMethod method)
    {
        WindowSize = Math.Clamp(windowSize, 1, MaximumWindowSize);
        Method = method;
        TrimToWindow();
    }

    public void Clear()
    {
        _distanceSamples.Clear();
        _angleSamples.Clear();
    }

    public SmoothedMeasurement Add(double distanceM, double angleDeg)
    {
        if (!double.IsFinite(distanceM) || !double.IsFinite(angleDeg))
            throw new ArgumentOutOfRangeException(nameof(distanceM), "Smoothing samples must be finite numbers.");

        _distanceSamples.Enqueue(distanceM);
        _angleSamples.Enqueue(angleDeg);
        TrimToWindow();
        return Current();
    }

    public SmoothedMeasurement Current()
    {
        if (SampleCount == 0)
            throw new InvalidOperationException("No samples are available for smoothing.");

        return new SmoothedMeasurement(
            Aggregate(_distanceSamples),
            Aggregate(_angleSamples),
            SampleCount);
    }

    private void TrimToWindow()
    {
        while (_distanceSamples.Count > WindowSize)
            _distanceSamples.Dequeue();
        while (_angleSamples.Count > WindowSize)
            _angleSamples.Dequeue();
    }

    private double Aggregate(IEnumerable<double> values) =>
        Method == SmoothingMethod.Median ? Median(values) : values.Average();

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        return ordered.Length % 2 == 1
            ? ordered[ordered.Length / 2]
            : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2.0;
    }
}
