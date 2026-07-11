using CommunityToolkit.Mvvm.ComponentModel;
using DudiverMusic.Models;

namespace DudiverMusic.ViewModels;

public partial class TrackViewModel : ObservableObject
{
    public Track Model { get; }

    public TrackViewModel(Track model)
    {
        Model = model;
        _title = model.Title;
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private bool _isPlaying;

    public string FilePath => Model.FilePath;

    public string DurationText =>
        Model.DurationSeconds > 0
            ? TimeSpan.FromSeconds(Model.DurationSeconds).ToString(Model.DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss")
            : "--:--";

    partial void OnTitleChanged(string value) => Model.Title = value;

    public string? Artist => Model.Artist;

    public void SetDuration(double seconds)
    {
        Model.DurationSeconds = seconds;
        OnPropertyChanged(nameof(DurationText));
    }

    public void SetTags(string? artist, string? album)
    {
        Model.Artist = string.IsNullOrWhiteSpace(artist) ? Model.Artist : artist;
        Model.Album = string.IsNullOrWhiteSpace(album) ? Model.Album : album;
        OnPropertyChanged(nameof(Artist));
    }
}
