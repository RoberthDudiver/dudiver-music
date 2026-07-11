namespace DudiverMusic.ViewModels;

/// <summary>Modo de repetición de la barra de reproducción.</summary>
public enum RepeatMode
{
    /// <summary>Sin repetición: al terminar la playlist, se detiene.</summary>
    Off = 0,

    /// <summary>Repite toda la playlist (loop).</summary>
    All = 1,

    /// <summary>Repite la canción actual.</summary>
    One = 2,
}
