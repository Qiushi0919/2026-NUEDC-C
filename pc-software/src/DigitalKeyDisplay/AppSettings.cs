using System.Text.Json;

namespace DigitalKeyLab;

public sealed class AppSettings
{
    public const string AnchorPortName = "COM22";
    public const string ControlPortName = "COM21";

    public int KeyIdentityId { get; set; }
    public decimal DistanceOffsetM { get; set; }
    public decimal AngleOffsetDeg { get; set; }
    public bool CalibrationModelEnabled { get; set; } = true;
    public int CalibrationModelVersion { get; set; } = CalibrationModel.Version;
    public bool MedianFilterEnabled { get; set; } = true;
    public int OutputSmoothingWindow { get; set; } = MeasurementSmoother.DefaultWindowSize;
    public string OutputSmoothingMethod { get; set; } = nameof(SmoothingMethod.Median);
    public int OutputSmoothingVersion { get; set; } = 1;
    public bool SoundEnabled { get; set; } = true;
    public bool AutoConnect { get; set; } = true;
    public string PreferredPort { get; set; } = AnchorPortName;
    public string PreferredControlPort { get; set; } = ControlPortName;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DigitalKeyLab",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty(nameof(KeyIdentityId), out _) &&
                document.RootElement.TryGetProperty("ExpectedFourBitId", out var legacyId) &&
                legacyId.TryGetInt32(out var value))
            {
                settings.KeyIdentityId = value;
            }
            var needsCalibrationMigration =
                !document.RootElement.TryGetProperty(nameof(CalibrationModelVersion), out var modelVersion) ||
                !modelVersion.TryGetInt32(out var savedVersion) || savedVersion < CalibrationModel.Version;
            if (needsCalibrationMigration)
            {
                // Version 3 displays the base-station center distance directly and
                // retains the field-confirmed piecewise angle mapping. Keep the
                // numeric controls as post-calibration fine trims.
                settings.DistanceOffsetM = 0;
                settings.AngleOffsetDeg = 0;
                settings.CalibrationModelEnabled = true;
                settings.CalibrationModelVersion = CalibrationModel.Version;
                settings.Save();
            }
            var needsSmoothingMigration =
                !document.RootElement.TryGetProperty(nameof(OutputSmoothingVersion), out var smoothingVersion) ||
                !smoothingVersion.TryGetInt32(out var savedSmoothingVersion) || savedSmoothingVersion < 1;
            if (needsSmoothingMigration)
            {
                settings.OutputSmoothingWindow = MeasurementSmoother.DefaultWindowSize;
                settings.OutputSmoothingMethod = nameof(SmoothingMethod.Median);
                settings.OutputSmoothingVersion = 1;
                settings.Save();
            }
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var folder = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(folder);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
