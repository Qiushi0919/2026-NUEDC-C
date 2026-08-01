namespace DigitalKeyLab;

public readonly record struct CalibrationInterval(double Minimum, double Maximum, bool IncludeMaximum = false)
{
    public bool Contains(double value) =>
        value >= Minimum && (value < Maximum || (IncludeMaximum && value <= Maximum));
}

public sealed class GlobalCalibrationAdjustment
{
    public int AngleRangeIndex { get; set; }
    public int DistanceRangeIndex { get; set; }
    public decimal DistanceCorrectionM { get; set; }
    public decimal AngleCorrectionDeg { get; set; }
}

public readonly record struct GlobalCalibrationResult(
    double DistanceM,
    double AngleDeg,
    int AngleRangeIndex,
    int DistanceRangeIndex)
{
    public bool HasMatchingRange => AngleRangeIndex >= 0 && DistanceRangeIndex >= 0;
}

public static class GlobalCalibrationModel
{
    public const decimal MinimumCorrection = -99.9m;
    public const decimal MaximumCorrection = 99.9m;

    public static IReadOnlyList<CalibrationInterval> AngleRanges { get; } = new[]
    {
        new CalibrationInterval(-50, -40),
        new CalibrationInterval(-40, -30),
        new CalibrationInterval(-30, -20),
        new CalibrationInterval(-20, -10),
        new CalibrationInterval(-10, 0),
        new CalibrationInterval(0, 10),
        new CalibrationInterval(10, 20),
        new CalibrationInterval(20, 30),
        new CalibrationInterval(30, 40),
        new CalibrationInterval(40, 50, true)
    };

    public static IReadOnlyList<CalibrationInterval> DistanceRanges { get; } = new[]
    {
        new CalibrationInterval(0, 0.50),
        new CalibrationInterval(0.50, 1.00),
        new CalibrationInterval(1.00, 1.50),
        new CalibrationInterval(1.50, 2.00),
        new CalibrationInterval(2.00, 2.50),
        new CalibrationInterval(2.50, 3.00),
        new CalibrationInterval(3.00, 3.50, true)
    };

    public static List<GlobalCalibrationAdjustment> CreateDefaultAdjustments() =>
        Normalize(null);

    public static List<GlobalCalibrationAdjustment> Normalize(
        IEnumerable<GlobalCalibrationAdjustment>? adjustments)
    {
        var saved = (adjustments ?? Array.Empty<GlobalCalibrationAdjustment>())
            .Where(item => item.AngleRangeIndex >= 0 && item.AngleRangeIndex < AngleRanges.Count &&
                           item.DistanceRangeIndex >= 0 && item.DistanceRangeIndex < DistanceRanges.Count)
            .GroupBy(item => (item.AngleRangeIndex, item.DistanceRangeIndex))
            .ToDictionary(group => group.Key, group => group.Last());

        var result = new List<GlobalCalibrationAdjustment>(AngleRanges.Count * DistanceRanges.Count);
        for (var angleIndex = 0; angleIndex < AngleRanges.Count; angleIndex++)
        {
            for (var distanceIndex = 0; distanceIndex < DistanceRanges.Count; distanceIndex++)
            {
                saved.TryGetValue((angleIndex, distanceIndex), out var item);
                result.Add(new GlobalCalibrationAdjustment
                {
                    AngleRangeIndex = angleIndex,
                    DistanceRangeIndex = distanceIndex,
                    DistanceCorrectionM = ClampCorrection(item?.DistanceCorrectionM ?? 0),
                    AngleCorrectionDeg = ClampCorrection(item?.AngleCorrectionDeg ?? 0)
                });
            }
        }
        return result;
    }

    public static GlobalCalibrationResult Apply(
        double rawDistanceM,
        double rawAngleDeg,
        IReadOnlyList<GlobalCalibrationAdjustment> adjustments)
    {
        if (!double.IsFinite(rawDistanceM) || !double.IsFinite(rawAngleDeg))
            throw new ArgumentOutOfRangeException(nameof(rawDistanceM), "Global calibration inputs must be finite.");

        var angleIndex = FindRange(AngleRanges, rawAngleDeg);
        var distanceIndex = FindRange(DistanceRanges, rawDistanceM);
        if (angleIndex < 0 || distanceIndex < 0)
            return new GlobalCalibrationResult(Math.Max(0, rawDistanceM), rawAngleDeg, -1, -1);

        var adjustment = adjustments.FirstOrDefault(item =>
            item.AngleRangeIndex == angleIndex && item.DistanceRangeIndex == distanceIndex);
        var distanceCorrection = (double)(adjustment?.DistanceCorrectionM ?? 0);
        var angleCorrection = (double)(adjustment?.AngleCorrectionDeg ?? 0);
        return new GlobalCalibrationResult(
            Math.Max(0, rawDistanceM + distanceCorrection),
            rawAngleDeg + angleCorrection,
            angleIndex,
            distanceIndex);
    }

    public static decimal AddCorrection(decimal current, decimal delta) =>
        ClampCorrection(current + delta);

    public static void ClearAll(IList<GlobalCalibrationAdjustment> adjustments)
    {
        foreach (var adjustment in adjustments)
        {
            adjustment.DistanceCorrectionM = 0;
            adjustment.AngleCorrectionDeg = 0;
        }
    }

    public static string FormatAngleRange(int index)
    {
        var range = AngleRanges[index];
        return $"{range.Minimum:0}～{range.Maximum:0}";
    }

    public static string FormatDistanceRange(int index)
    {
        var range = DistanceRanges[index];
        return $"{range.Minimum:0.00}～{range.Maximum:0.00}";
    }

    private static int FindRange(IReadOnlyList<CalibrationInterval> ranges, double value)
    {
        for (var index = 0; index < ranges.Count; index++)
        {
            if (ranges[index].Contains(value))
                return index;
        }
        return -1;
    }

    private static decimal ClampCorrection(decimal value) =>
        Math.Clamp(decimal.Round(value, 2, MidpointRounding.AwayFromZero),
            MinimumCorrection, MaximumCorrection);
}
