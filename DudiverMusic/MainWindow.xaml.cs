using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using DudiverMusic.ViewModels;

namespace DudiverMusic;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        StateChanged += (_, _) => MaxBtn.Content = WindowState == WindowState.Maximized ? "" : "";
        Closed += (_, _) => { _vm.Persist(); _vm.Dispose(); };
    }

    // ===================== Title bar =====================

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnAboutClick(object sender, RoutedEventArgs e) =>
        new Views.AboutWindow { Owner = this }.ShowDialog();

    private void OnDevicePopupOpen(object sender, RoutedEventArgs e) => _vm.RefreshDevices();

    // ===================== Reproducir / reordenar canciones =====================

    private Point _dragStart;
    private TrackViewModel? _pressItem;
    private bool _dragging;

    private void OnTrackListMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        if (IsInButton(e.OriginalSource)) { _pressItem = null; return; }
        _dragStart = e.GetPosition(TrackList);
        _pressItem = ItemUnder(e.OriginalSource);
    }

    private void OnTrackListMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging || _pressItem is null || e.LeftButton != MouseButtonState.Pressed || !_vm.CanReorder) return;
        var p = e.GetPosition(TrackList);
        if (Math.Abs(p.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(p.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        _dragging = true;
        DragDrop.DoDragDrop(TrackList, _pressItem, DragDropEffects.Move);
        _pressItem = null;   // consumido por el drag
    }

    private void OnTrackListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging && _pressItem is not null)
            _vm.PlayTrackCommand.Execute(_pressItem);
        _pressItem = null;
    }

    private void OnTrackListDrop(object sender, DragEventArgs e)
    {
        if (!_vm.CanReorder || _vm.SelectedPlaylist is not { } pl) return;
        if (e.Data.GetData(typeof(TrackViewModel)) is not TrackViewModel dragged) return;
        var target = ItemUnder(e.OriginalSource);
        if (target is null || target == dragged) return;
        pl.MoveTrack(pl.Tracks.IndexOf(dragged), pl.Tracks.IndexOf(target));
        _vm.Persist();
    }

    private void OnDeletePlaylist(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PlaylistViewModel pl })
        {
            var msg = string.Format(Localization.LocalizationManager.Tr("DeleteText"), pl.Name);
            if (Views.ConfirmDialog.Show(Localization.LocalizationManager.Tr("DeleteTitle"), msg))
                _vm.RemovePlaylistCommand.Execute(pl);
        }
    }

    private static TrackViewModel? ItemUnder(object? source)
    {
        var d = source as DependencyObject;
        while (d is not null and not ListBoxItem) d = VisualTreeHelper.GetParent(d);
        return (d as ListBoxItem)?.DataContext as TrackViewModel;
    }

    private static bool IsInButton(object? source)
    {
        var d = source as DependencyObject;
        while (d is not null and not ListBoxItem)
        {
            if (d is Button) return true;
            d = VisualTreeHelper.GetParent(d);
        }
        return false;
    }

    // ===================== Drag & drop de carpetas =====================

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        if (DropHint is not null) DropHint.Opacity = 0.6;
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        if (DropHint is not null) DropHint.Opacity = 1;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DropHint is not null) DropHint.Opacity = 1;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        _vm.HandleDroppedPaths(paths);
    }

    private void OnAddFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = Localization.LocalizationManager.Tr("DlgChooseFolderTitle"),
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true)
            _vm.HandleDroppedPaths(dialog.FolderNames);
    }

    private void OnAddFiles(object sender, RoutedEventArgs e)
    {
        var audio = Localization.LocalizationManager.Tr("DlgAudioFilter");
        var all = Localization.LocalizationManager.Tr("DlgAllFiles");
        var dialog = new OpenFileDialog
        {
            Title = Localization.LocalizationManager.Tr("DlgChooseFilesTitle"),
            Multiselect = true,
            Filter = $"{audio} (*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.aiff;*.aif;*.ogg;*.opus)" +
                     "|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.aiff;*.aif;*.ogg;*.opus" +
                     $"|{all} (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            _vm.AddFiles(dialog.FileNames, appendToSelected: true);
    }

    // ===================== Renombrar playlist =====================

    private void OnRenameVisible(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox { IsVisible: true } box)
        {
            box.Focus();
            box.SelectAll();
        }
    }

    private void OnRenameKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { Tag: PlaylistViewModel pl }) return;

        if (e.Key == Key.Enter) { CommitRename(pl); e.Handled = true; }
        else if (e.Key == Key.Escape) { pl.IsRenaming = false; e.Handled = true; }
    }

    private void OnRenameLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { Tag: PlaylistViewModel pl }) CommitRename(pl);
    }

    private void CommitRename(PlaylistViewModel pl)
    {
        pl.IsRenaming = false;
        _vm.Persist();
    }
}
