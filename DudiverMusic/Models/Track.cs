using System.IO;

namespace DudiverMusic.Models;

/// <summary>Una canción dentro de una playlist.</summary>
public sealed class Track
{
    public required string FilePath { get; init; }

    /// <summary>Título mostrado (por defecto, el nombre del archivo sin extensión).</summary>
    public string Title { get; set; } = "";

    public string? Artist { get; set; }
    public string? Album { get; set; }

    /// <summary>Duración en segundos (0 si aún no se leyó).</summary>
    public double DurationSeconds { get; set; }

    public static Track FromFile(string path) => new()
    {
        FilePath = path,
        Title = Path.GetFileNameWithoutExtension(path)
    };
}
