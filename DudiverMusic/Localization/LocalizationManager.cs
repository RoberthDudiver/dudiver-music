using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace DudiverMusic.Localization;

/// <summary>
/// Localización con cambio en vivo. Las vistas se enlazan al indexador
/// <c>[clave]</c>; al cambiar de idioma se refrescan solas.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private string _lang = "es";

    private LocalizationManager() { }

    public string CurrentCode => _lang;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public string this[string key] =>
        (_lang == "en" ? En : Es).TryGetValue(key, out var v) ? v : key;

    public static string Tr(string key) => Instance[key];

    /// <summary>Resuelve "" / null como automático según la cultura del sistema.</summary>
    public void SetLanguage(string? code)
    {
        var resolved = code switch
        {
            "es" or "en" => code,
            _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es" ? "es" : "en"
        };
        if (resolved == _lang) return;
        _lang = resolved;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private static readonly Dictionary<string, string> Es = new()
    {
        ["AppTagline"] = "Reproductor de música moderno y sencillo.",
        ["TipAbout"] = "Acerca de",
        ["TipMinimize"] = "Minimizar",
        ["TipMaximize"] = "Maximizar",
        ["TipClose"] = "Cerrar",
        ["TipLanguage"] = "Idioma",
        ["YourPlaylists"] = "TUS PLAYLISTS",
        ["Folder"] = "Carpeta",
        ["Files"] = "Archivos",
        ["TipAddFolder"] = "Agregar una carpeta como playlist",
        ["TipAddFiles"] = "Agregar archivos sueltos",
        ["TipRename"] = "Renombrar",
        ["TipDeletePlaylist"] = "Eliminar playlist",
        ["PlaylistEyebrow"] = "PLAYLIST",
        ["DropTitle"] = "Arrastrá una carpeta o archivos de música acá",
        ["DropSubtitle"] = "Con una carpeta se crea una playlist con su nombre",
        ["ChooseFolder"] = "Elegir carpeta",
        ["ChooseFiles"] = "Elegir archivos",
        ["NothingPlaying"] = "Nada sonando",
        ["TipShuffle"] = "Aleatorio",
        ["TipPrevious"] = "Anterior",
        ["TipNext"] = "Siguiente",
        ["TipRepeat"] = "Repetir",
        ["TipRepeatOff"] = "Repetir: desactivado",
        ["TipRepeatAll"] = "Repetir la playlist",
        ["TipRepeatOne"] = "Repetir esta canción",
        ["TipNewPlaylist"] = "Nueva playlist",
        ["NewPlaylistName"] = "Nueva playlist",
        ["TipMute"] = "Silenciar",
        ["TipOutput"] = "Salida de audio",
        ["OutputHeader"] = "SALIDA DE AUDIO",
        ["OutputDefault"] = "Predeterminado del sistema",
        ["SongsCountOne"] = "1 canción",
        ["SongsCountMany"] = "{0} canciones",
        ["DefaultPlaylistName"] = "Mis canciones",
        ["DlgChooseFolderTitle"] = "Elegí una carpeta con música",
        ["DlgChooseFilesTitle"] = "Elegí archivos de música",
        ["DlgAudioFilter"] = "Audio",
        ["DlgAllFiles"] = "Todos los archivos",
        ["AboutTitle"] = "Acerca de Dudiver Music",
        ["Version"] = "Versión 1.0.0",
        ["MadeBy"] = "Hecho con código y beats por",
        ["MadeByFrom"] = "desde Venezuela 🇻🇪",
        ["LicenseHeader"] = "LICENCIA",
        ["LicenseText"] = "Gratis para uso personal y profesional. No puede ser vendido ni redistribuido con fines de lucro. No elimines los créditos del autor.",
        ["AboutStory"] = "Hecho por un músico, para escuchar música rápido y fácil, sin depender de sistemas complejos. Lo creé para escuchar mi disco antes de lanzarlo —como sonaría en las plataformas— y lo comparto para gente con la misma necesidad.",
        ["Website"] = "Sitio web",
        ["Donate"] = "Donar con PayPal",
        ["TermsHeader"] = "TÉRMINOS",
        ["TermsText"] = "Dudiver Music reproduce archivos de audio que ya están en tu computadora. No aloja, distribuye ni provee música: solo lee tus archivos locales. Sos responsable de los archivos que reproducís y de tener los derechos para hacerlo. El autor no se hace responsable del contenido que cada usuario decida escuchar. El software se ofrece «tal cual», sin garantías.",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["AppTagline"] = "A modern, simple music player.",
        ["TipAbout"] = "About",
        ["TipMinimize"] = "Minimize",
        ["TipMaximize"] = "Maximize",
        ["TipClose"] = "Close",
        ["TipLanguage"] = "Language",
        ["YourPlaylists"] = "YOUR PLAYLISTS",
        ["Folder"] = "Folder",
        ["Files"] = "Files",
        ["TipAddFolder"] = "Add a folder as a playlist",
        ["TipAddFiles"] = "Add individual files",
        ["TipRename"] = "Rename",
        ["TipDeletePlaylist"] = "Delete playlist",
        ["PlaylistEyebrow"] = "PLAYLIST",
        ["DropTitle"] = "Drag a music folder or files here",
        ["DropSubtitle"] = "A folder becomes a playlist with its name",
        ["ChooseFolder"] = "Choose folder",
        ["ChooseFiles"] = "Choose files",
        ["NothingPlaying"] = "Nothing playing",
        ["TipShuffle"] = "Shuffle",
        ["TipPrevious"] = "Previous",
        ["TipNext"] = "Next",
        ["TipRepeat"] = "Repeat",
        ["TipRepeatOff"] = "Repeat: off",
        ["TipRepeatAll"] = "Repeat the playlist",
        ["TipRepeatOne"] = "Repeat this song",
        ["TipNewPlaylist"] = "New playlist",
        ["NewPlaylistName"] = "New playlist",
        ["TipMute"] = "Mute",
        ["TipOutput"] = "Audio output",
        ["OutputHeader"] = "AUDIO OUTPUT",
        ["OutputDefault"] = "System default",
        ["SongsCountOne"] = "1 song",
        ["SongsCountMany"] = "{0} songs",
        ["DefaultPlaylistName"] = "My songs",
        ["DlgChooseFolderTitle"] = "Choose a music folder",
        ["DlgChooseFilesTitle"] = "Choose music files",
        ["DlgAudioFilter"] = "Audio",
        ["DlgAllFiles"] = "All files",
        ["AboutTitle"] = "About Dudiver Music",
        ["Version"] = "Version 1.0.0",
        ["MadeBy"] = "Made with code and beats by",
        ["MadeByFrom"] = "from Venezuela 🇻🇪",
        ["LicenseHeader"] = "LICENSE",
        ["LicenseText"] = "Free for personal and professional use. It cannot be sold or redistributed for profit. Don't remove the author's credits.",
        ["AboutStory"] = "Made by a musician, to listen to music fast and easy, without relying on complex systems. I built it to hear my album before releasing it —how it would sound on the platforms— and I share it for people with the same need.",
        ["Website"] = "Website",
        ["Donate"] = "Donate with PayPal",
        ["TermsHeader"] = "TERMS",
        ["TermsText"] = "Dudiver Music plays audio files that are already on your computer. It does not host, distribute or provide any music: it only reads your local files. You are responsible for the files you play and for having the rights to do so. The author is not responsible for the content each user chooses to listen to. The software is provided “as is”, without warranties.",
    };
}
