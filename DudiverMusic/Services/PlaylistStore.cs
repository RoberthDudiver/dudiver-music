using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DudiverMusic.Models;

namespace DudiverMusic.Services;

/// <summary>Guarda y carga las playlists en %LocalAppData%\DudiverMusic\library.json.</summary>
public static class PlaylistStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DudiverMusic");

    private static readonly string FilePath = Path.Combine(Dir, "library.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<Playlist> Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new List<Playlist>();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<Playlist>>(json) ?? new List<Playlist>();
        }
        catch
        {
            return new List<Playlist>();
        }
    }

    public static void Save(IEnumerable<Playlist> playlists)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(playlists, Options);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Silencioso: no queremos romper la app por un fallo de guardado.
        }
    }
}
