using System.IO;
using System.Text.Json;

namespace DudiverMusic.Services;

public sealed class AppSettings
{
    public string OutputDeviceId { get; set; } = AudioDevice.DefaultId;
    public double Volume { get; set; } = 0.8;

    /// <summary>"es", "en" o "" (automático según el sistema).</summary>
    public string Language { get; set; } = "";

    public bool Shuffle { get; set; }

    /// <summary>0 = off, 1 = repetir playlist, 2 = repetir una.</summary>
    public int Repeat { get; set; }
}

/// <summary>Preferencias en %LocalAppData%\DudiverMusic\settings.json.</summary>
public static class SettingsStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DudiverMusic");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch { /* no romper la app por un fallo de guardado */ }
    }
}
