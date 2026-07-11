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

    /// <summary>Agrega canciones a la playlist (usado al soltar/abrir archivos sueltos).</summary>
    public void AddTracks(IEnumerable<Track> tracks)
    {
        foreach (var t in tracks)
        {
            Model.Tracks.Add(t);
            Tracks.Add(new TrackViewModel(t));
        }
        OnPropertyChanged(nameof(TrackCountText));
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = string.IsNullOrWhiteSpace(value) ? Model.Name : value.Trim();
    }
}
