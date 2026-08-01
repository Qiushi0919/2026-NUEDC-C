namespace DigitalKeyLab;

public readonly record struct AveragedRawMeasurement(
    double DistanceM,
    double AngleDeg,
    int SourceFrameCount);

public sealed class ValidFrameAverager
{
    private readonly int _framesPerOutput;
    private int _validFrameCount;
    private double _distanceSum;
    private double _angleSum;

    public ValidFrameAverager(int framesPerOutput)
    {
        if (framesPerOutput < 1)
            throw new ArgumentOutOfRangeException(nameof(framesPerOutput));
        _framesPerOutput = framesPerOutput;
    }

    public bool TryAdd(
        double rawDistanceM,
        double rawAngleDeg,
        out AveragedRawMeasurement averaged)
    {
        if (!double.IsFinite(rawDistanceM) || !double.IsFinite(rawAngleDeg))
            throw new ArgumentOutOfRangeException(nameof(rawDistanceM), "Raw frame values must be finite.");

        _distanceSum += rawDistanceM;
        _angleSum += rawAngleDeg;
        _validFrameCount++;
        if (_validFrameCount < _framesPerOutput)
        {
            averaged = default;
            return false;
        }

        averaged = new AveragedRawMeasurement(
            _distanceSum / _validFrameCount,
            _angleSum / _validFrameCount,
            _validFrameCount);
        Reset();
        return true;
    }

    public void Reset()
    {
        _validFrameCount = 0;
        _distanceSum = 0;
        _angleSum = 0;
    }
}
