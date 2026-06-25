using KaraokeApp.MVVM;

namespace KaraokeApp.ViewModels;

public class PlaylistItemViewModel : BaseViewModel
{
    private int _position;
    public int Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    private SongViewModel? _song;
    public SongViewModel? Song
    {
        get => _song;
        set => SetProperty(ref _song, value);
    }
}