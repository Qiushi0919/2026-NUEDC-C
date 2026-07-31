using System.Text.Json;

namespace DigitalKeyLab;

public sealed class AppSettings
{
    public int ExpectedFourBitId { get; set; }
    public decimal DistanceOffsetM { get; set; }
    public decimal AngleOffsetDeg { get; set; }
    public bool MedianFilterEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool AutoConnect { get; set; } = true;
    public string PreferredPort { get; set; } = "COM8";
    public string PreferredControlPort { get; set; } = "COM1";

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DigitalKeyLab",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings()
                : new AppSettings();
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
