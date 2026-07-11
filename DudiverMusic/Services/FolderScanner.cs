using System.Collections.Generic;
using System.IO;
using System.Linq;
using DudiverMusic.Models;

namespace DudiverMusic.Services;

/// <summary>Busca archivos de audio dentro de una carpeta.</summary>
public static class FolderScanner
{
    /// <summary>Formatos de audio soportados (reproducibles por Windows Media Foundation).</summary>
    public static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma", ".aiff", ".aif", ".ogg", ".opus"
    };

    public static bool IsAudioFile(string path) =>
        AudioExtensions.Contains(Path.GetExtension(path));

    public static bool HasAudio(string folder)
    {
        try
        {
            return EnumerateAudio(folder).Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Crea una playlist con el nombre de la carpeta y sus archivos de audio (orden natural).</summary>
    public static Playlist BuildPlaylist(string folder)
    {
        var files = EnumerateAudio(folder)
            .OrderBy(Path.GetDirectoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f, NaturalComparer.Instance)
            .Select(Track.FromFile)
            .ToList();

        return new Playlist
        {
            Name = new DirectoryInfo(folder).Name,
            SourceFolder = folder,
            Tracks = files
        };
    }

    private static IEnumerable<string> EnumerateAudio(string folder) =>
        Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                 .Where(f => AudioExtensions.Contains(Path.GetExtension(f)));
}
