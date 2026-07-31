using System.Text.Json;

namespace DigitalKeyLab;

public sealed class AppSettings
{
    public int KeyIdentityId { get; set; }
    public decimal DistanceOffsetM { get; set; }
    public decimal AngleOffsetDeg { get; set; }
    public bool MedianFilterEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool AutoConnect { get; set; } = true;
    public string PreferredPort { get; set; } = "COM22";
    public string PreferredControlPort { get; set; } = "COM1";

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
