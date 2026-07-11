using System.Collections.Generic;

namespace DudiverMusic.Models;

/// <summary>Una playlist creada al arrastrar una carpeta.</summary>
public sealed class Playlist
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    /// <summary>Carpeta de origen (para re-escanear si se quiere).</summary>
    public string? SourceFolder { get; set; }

    public List<Track> Tracks { get; set; } = new();
}
