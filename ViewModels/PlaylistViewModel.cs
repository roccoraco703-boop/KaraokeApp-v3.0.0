using System.Collections.ObjectModel;

namespace KaraokeApp.ViewModels;

public class PlaylistViewModel
{
    public ObservableCollection<
        PlaylistItemViewModel>
        Playlist { get; }
        = new();

    public PlaylistItemViewModel?
        SelectedPlaylistItem
        { get; set; }
}