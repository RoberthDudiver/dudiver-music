using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DudiverMusic.Localization;
using DudiverMusic.Models;
using DudiverMusic.Services;

namespace DudiverMusic.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    // Glifos (Segoe MDL2 Assets / Segoe Fluent Icons)
    private const string GlyphPlay = "";
    private const string GlyphPause = "";
    private const string GlyphMute = "";
    private const string GlyphVolHigh = "";
    private const string GlyphVolLow = "";

    private readonly AudioPlayer _player = new();
    private readonly AppSettings _settings;
    private bool _updatingFromPlayer;
    private double _volumeBeforeMute = 0.8;
    private readonly Random _rng = new();

    public MainViewModel()
    {
        _player.PositionChanged += (_, pos) =>
        {
            _updatingFromPlayer = true;
            PositionSeconds = pos.TotalSeconds;
            _updatingFromPlayer = false;
        };
        _player.MediaOpened += (_, _) =>
        {
            DurationSeconds = _player.Duration.TotalSeconds;
            CurrentTrack?.SetDuration(DurationSeconds);
        };
        _player.MediaEnded += (_, _) => OnTrackFinished();
        _player.PlayStateChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(PlayPauseGlyph));
            if (CurrentTrack is not null) CurrentTrack.IsPlaying = _player.IsPlaying;
        };

        foreach (var pl in PlaylistStore.Load())
            Playlists.Add(new PlaylistViewModel(pl));

        SelectedPlaylist = Playlists.FirstOrDefault();

        _settings = SettingsStore.Load();
        _volume = _settings.Volume;
        _player.Volume = _volume;
        _isShuffle = _settings.Shuffle;
        _repeat = (RepeatMode)Math.Clamp(_settings.Repeat, 0, 2);
        _player.SetOutputDevice(_settings.OutputDeviceId);
        RefreshDevices();

        _selectedLanguage = Languages.FirstOrDefault(l => l.Code == LocalizationManager.Instance.CurrentCode)
                            ?? Languages[0];
        LocalizationManager.Instance.LanguageChanged += (_, _) =>
        {
            RefreshDevices();
            OnPropertyChanged(nameof(NowPlayingTitle));
            OnPropertyChanged(nameof(RepeatTooltip));
        };
    }

    // ===================== Idioma =====================

    public sealed record LanguageOption(string Code, string Name);

    public ObservableCollection<LanguageOption> Languages { get; } = new()
    {
        new("es", "Español"),
        new("en", "English"),
    };

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is null) return;
        LocalizationManager.Instance.SetLanguage(value.Code);
        if (_settings is not null)
        {
            _settings.Language = value.Code;
            SettingsStore.Save(_settings);
        }
    }

    public string NowPlayingTitle =>
        CurrentTrack?.Title ?? LocalizationManager.Tr("NothingPlaying");

    partial void OnCurrentTrackChanged(TrackViewModel? value) =>
        OnPropertyChanged(nameof(NowPlayingTitle));

    // ===================== Biblioteca =====================

    public ObservableCollection<PlaylistViewModel> Playlists { get; } = new();

    [ObservableProperty]
    private PlaylistViewModel? _selectedPlaylist;

    public bool HasPlaylists => Playlists.Count > 0;

    /// <summary>La playlist que se está reproduciendo (puede diferir de la seleccionada).</summary>
    private PlaylistViewModel? _playingPlaylist;

    // ===================== Estado de reproducción =====================

    [ObservableProperty]
    private TrackViewModel? _currentTrack;

    public bool IsPlaying => _player.IsPlaying;
    public string PlayPauseGlyph => _player.IsPlaying ? GlyphPause : GlyphPlay;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private double _durationSeconds;

    public string PositionText => Format(PositionSeconds);
    public string DurationText => Format(DurationSeconds);

    partial void OnPositionSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(PositionText));
        if (!_updatingFromPlayer && _player.HasMedia)
            _player.Seek(value);
    }

    partial void OnDurationSecondsChanged(double value) => OnPropertyChanged(nameof(DurationText));

    // ===================== Volumen =====================

    [ObservableProperty]
    private double _volume;

    public string VolumeGlyph => Volume <= 0.001 ? GlyphMute : Volume < 0.5 ? GlyphVolLow : GlyphVolHigh;

    partial void OnVolumeChanged(double value)
    {
        _player.Volume = value;
        OnPropertyChanged(nameof(VolumeGlyph));
        if (_settings is not null)
        {
            _settings.Volume = value;
            SettingsStore.Save(_settings);
        }
    }

    // ===================== Salida de audio =====================

    public ObservableCollection<AudioDevice> OutputDevices { get; } = new();

    [ObservableProperty]
    private AudioDevice? _selectedOutputDevice;

    partial void OnSelectedOutputDeviceChanged(AudioDevice? value)
    {
        if (value is null) return;
        _player.SetOutputDevice(value.Id);
        if (_settings is not null)
        {
            _settings.OutputDeviceId = value.Id;
            SettingsStore.Save(_settings);
        }
    }

    /// <summary>Recarga la lista de salidas (llamar al abrir el selector; pueden conectarse/quitarse).</summary>
    public void RefreshDevices()
    {
        var currentId = _player.CurrentDeviceId;
        OutputDevices.Clear();
        AudioDevice? current = null;
        foreach (var d in _player.GetOutputDevices())
        {
            var dev = d.IsDefault ? d with { Name = LocalizationManager.Tr("OutputDefault") } : d;
            OutputDevices.Add(dev);
            if (dev.Id == currentId) current = dev;
        }
        SelectedOutputDevice = current ?? OutputDevices.FirstOrDefault();
    }

    // ===================== Shuffle / Repeat =====================

    // Glifos de repetición
    private const string GlyphRepeatAll = "";
    private const string GlyphRepeatOne = "";

    [ObservableProperty]
    private bool _isShuffle;

    partial void OnIsShuffleChanged(bool value)
    {
        if (_settings is not null) { _settings.Shuffle = value; SettingsStore.Save(_settings); }
    }

    [ObservableProperty]
    private RepeatMode _repeat;

    partial void OnRepeatChanged(RepeatMode value)
    {
        OnPropertyChanged(nameof(RepeatGlyph));
        OnPropertyChanged(nameof(IsRepeatActive));
        OnPropertyChanged(nameof(RepeatTooltip));
        if (_settings is not null) { _settings.Repeat = (int)value; SettingsStore.Save(_settings); }
    }

    public bool IsRepeatActive => Repeat != RepeatMode.Off;
    public string RepeatGlyph => Repeat == RepeatMode.One ? GlyphRepeatOne : GlyphRepeatAll;
    public string RepeatTooltip => LocalizationManager.Tr(
        Repeat switch { RepeatMode.All => "TipRepeatAll", RepeatMode.One => "TipRepeatOne", _ => "TipRepeatOff" });

    [RelayCommand]
    private void CycleRepeat() =>
        Repeat = Repeat switch { RepeatMode.Off => RepeatMode.All, RepeatMode.All => RepeatMode.One, _ => RepeatMode.Off };

    // ===================== Comandos =====================

    [RelayCommand]
    private void PlayTrack(TrackViewModel? track)
    {
        if (track is null) return;
        _playingPlaylist = SelectedPlaylist;
        StartTrack(track);
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (CurrentTrack is null)
        {
            var first = SelectedPlaylist?.Tracks.FirstOrDefault();
            if (first is not null) { _playingPlaylist = SelectedPlaylist; StartTrack(first); }
            return;
        }
        _player.TogglePlayPause();
    }

    /// <summary>Fin de canción: en "repetir una" reinicia; si no, avanza.</summary>
    private void OnTrackFinished()
    {
        if (Repeat == RepeatMode.One && CurrentTrack is not null)
        {
            StartTrack(CurrentTrack);
            return;
        }
        AdvanceNext(auto: true);
    }

    [RelayCommand]
    private void Next() => AdvanceNext(auto: false);

    private void AdvanceNext(bool auto)
    {
        var list = _playingPlaylist?.Tracks;
        if (list is null || list.Count == 0 || CurrentTrack is null) return;

        int idx = list.IndexOf(CurrentTrack);
        int next;
        if (IsShuffle && list.Count > 1)
        {
            do { next = _rng.Next(list.Count); } while (next == idx);
        }
        else
        {
            next = idx + 1;
            if (next >= list.Count)
            {
                // Loop de la playlist si repetir está activo; al pulsar "siguiente"
                // manualmente también vuelve al principio.
                if (Repeat != RepeatMode.Off || !auto) next = 0;
                else { _player.Stop(); return; }
            }
        }
        StartTrack(list[next]);
    }

    [RelayCommand]
    private void Previous()
    {
        var list = _playingPlaylist?.Tracks;
        if (list is null || list.Count == 0 || CurrentTrack is null) return;

        // Si ya pasaron 3s, reinicia la canción actual.
        if (PositionSeconds > 3) { _player.Seek(0); return; }

        int idx = list.IndexOf(CurrentTrack);
        int prev = idx - 1;
        if (prev < 0) prev = Repeat != RepeatMode.Off ? list.Count - 1 : 0;
        StartTrack(list[prev]);
    }

    [RelayCommand]
    private void ToggleMute()
    {
        if (Volume > 0.001) { _volumeBeforeMute = Volume; Volume = 0; }
        else Volume = _volumeBeforeMute <= 0.001 ? 0.8 : _volumeBeforeMute;
    }

    [RelayCommand]
    private void StartRename(PlaylistViewModel? pl)
    {
        if (pl is not null) pl.IsRenaming = true;
    }

    [RelayCommand]
    private void RemovePlaylist(PlaylistViewModel? pl)
    {
        if (pl is null) return;
        Playlists.Remove(pl);
        if (SelectedPlaylist == pl) SelectedPlaylist = Playlists.FirstOrDefault();
        OnPropertyChanged(nameof(HasPlaylists));
        Persist();
    }

    /// <summary>Crea una playlist vacía desde cero, la selecciona y entra en modo renombrar.</summary>
    [RelayCommand]
    private void NewPlaylist()
    {
        var pl = new PlaylistViewModel(new Playlist { Name = LocalizationManager.Tr("NewPlaylistName") });
        Playlists.Add(pl);
        SelectedPlaylist = pl;
        OnPropertyChanged(nameof(HasPlaylists));
        pl.IsRenaming = true;
        Persist();
    }

    // ===================== Drag & drop de carpetas =====================

    /// <summary>Soltar carpetas (→ playlist por carpeta) y/o archivos sueltos (→ playlist actual o nueva).</summary>
    public void HandleDroppedPaths(IEnumerable<string> paths)
    {
        PlaylistViewModel? firstAdded = null;
        var looseFiles = new List<string>();

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                if (!FolderScanner.HasAudio(path)) continue;
                var playlist = new PlaylistViewModel(FolderScanner.BuildPlaylist(path));
                Playlists.Add(playlist);
                firstAdded ??= playlist;
            }
            else if (File.Exists(path) && FolderScanner.IsAudioFile(path))
            {
                looseFiles.Add(path);
            }
        }

        if (firstAdded is not null)
        {
            SelectedPlaylist = firstAdded;
            OnPropertyChanged(nameof(HasPlaylists));
            Persist();
        }

        if (looseFiles.Count > 0)
            AddFiles(looseFiles, appendToSelected: firstAdded is null);
    }

    /// <summary>
    /// Agrega archivos de audio. Por defecto crea una playlist nueva (nombre según la carpeta
    /// común si la comparten). Con <paramref name="appendToSelected"/> los suma a la seleccionada.
    /// </summary>
    public void AddFiles(IEnumerable<string> files, bool appendToSelected = false)
    {
        var paths = files.Where(FolderScanner.IsAudioFile)
                         .OrderBy(f => f, NaturalComparer.Instance)
                         .ToList();
        if (paths.Count == 0) return;

        var tracks = paths.Select(Track.FromFile).ToList();

        var target = appendToSelected ? SelectedPlaylist : null;
        if (target is null)
        {
            target = new PlaylistViewModel(new Playlist { Name = SuggestName(paths) });
            Playlists.Add(target);
            SelectedPlaylist = target;
            OnPropertyChanged(nameof(HasPlaylists));
        }

        target.AddTracks(tracks);
        Persist();
    }

    /// <summary>Si todos los archivos están en la misma carpeta, usa su nombre; si no, "Mis canciones".</summary>
    private static string SuggestName(IReadOnlyList<string> paths)
    {
        var dirs = paths.Select(Path.GetDirectoryName).Distinct().ToList();
        return dirs.Count == 1 && dirs[0] is { Length: > 0 } d
            ? new DirectoryInfo(d).Name
            : LocalizationManager.Tr("DefaultPlaylistName");
    }

    // ===================== Helpers =====================

    private void StartTrack(TrackViewModel track)
    {
        if (CurrentTrack is not null) { CurrentTrack.IsCurrent = false; CurrentTrack.IsPlaying = false; }

        CurrentTrack = track;
        track.IsCurrent = true;
        PositionSeconds = 0;
        DurationSeconds = 0;

        _player.Load(track.FilePath);
        _player.Play();
    }

    public void Persist() => PlaylistStore.Save(Playlists.Select(p => p.Model));

    private static string Format(double seconds)
    {
        if (seconds <= 0 || double.IsNaN(seconds)) return "0:00";
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
    }

    public void Dispose() => _player.Dispose();
}
