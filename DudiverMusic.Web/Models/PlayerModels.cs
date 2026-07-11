namespace DudiverMusic.Web.Models;

public sealed class Track
{
    public required string Id { get; init; }     // id en el registro JS
    public required string Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public double DurationSeconds { get; set; }

    public string DurationText => DurationSeconds > 0
        ? TimeSpan.FromSeconds(DurationSeconds).ToString(DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss")
        : "--:--";
}

public sealed class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<Track> Tracks { get; set; } = new();
    public bool IsRenaming { get; set; }
}

/// <summary>DTO que devuelve el interop JS al elegir/soltar archivos.</summary>
public sealed class PickedFile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";     // sin extensión (para mostrar)
    public string Folder { get; set; } = "";
    public string File { get; set; } = "";      // nombre real con extensión (para detectar formato)
}

public sealed record AudioDeviceInfo(string Id, string Name);
