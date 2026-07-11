using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DudiverMusic.Localization;
using DudiverMusic.Models;

namespace DudiverMusic.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    public Playlist Model { get; }

    public PlaylistViewModel(Playlist model)
    {
        Model = model;
        _name = model.Name;
        Tracks = new ObservableCollection<TrackViewModel>(model.Tracks.Select(t => new TrackViewModel(t)));
        LocalizationManager.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(TrackCountText));
    }

    public ObservableCollection<TrackViewModel> Tracks { get; }

    [ObservableProperty]
    private string _name;

    /// <summary>Modo edición del nombre (inline).</summary>
    [ObservableProperty]
    private bool _isRenaming;

    public string TrackCountText =>
        Tracks.Count == 1
            ? LocalizationManager.Tr("SongsCountOne")
            : string.Format(LocalizationManager.Tr("SongsCountMany"), Tracks.Count);

    /// <summary>Conteo + duración total (ej. "34 canciones · 2 h 12 min"). Duración solo si se leyó.</summary>
    public string MetaText
    {
        get
        {
            double total = Tracks.Sum(t => t.Model.DurationSeconds);
            if (total <= 0) return TrackCountText;
            var ts = TimeSpan.FromSeconds(total);
            var dur = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours} h {ts.Minutes} min" : $"{ts.Minutes} min";
            return $"{TrackCountText} · {dur}";
        }
    }

    public void RaiseMeta()
    {
        OnPropertyChanged(nameof(TrackCountText));
        OnPropertyChanged(nameof(MetaText));
    }

    /// <summary>Agrega canciones a la playlist (usado al soltar/abrir archivos sueltos).</summary>
    public void AddTracks(IEnumerable<Track> tracks)
    {
        foreach (var t in tracks)
        {
            Model.Tracks.Add(t);
            Tracks.Add(new TrackViewModel(t));
        }
        RaiseMeta();
    }

    /// <summary>Quita una pista de la playlist.</summary>
    public void RemoveTrack(TrackViewModel t)
    {
        Tracks.Remove(t);
        Model.Tracks.Remove(t.Model);
        RaiseMeta();
    }

    /// <summary>Mueve una pista (reordenar) en la vista y en el modelo.</summary>
    public void MoveTrack(int from, int to)
    {
        if (from < 0 || to < 0 || from >= Tracks.Count || to >= Tracks.Count || from == to) return;
        Tracks.Move(from, to);
        var m = Model.Tracks[from];
        Model.Tracks.RemoveAt(from);
        Model.Tracks.Insert(to, m);
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = string.IsNullOrWhiteSpace(value) ? Model.Name : value.Trim();
    }
}
