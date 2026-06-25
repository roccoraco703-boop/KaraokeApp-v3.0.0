using KaraokeApp.MVVM;

namespace KaraokeApp.ViewModels
{
    public class MidiChannelViewModel : BaseViewModel
    {
        private int _channelNumber;
        public int ChannelNumber
        {
            get => _channelNumber;
            set
            {
                if (SetProperty(ref _channelNumber, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        private string _instrumentName = string.Empty;
        public string InstrumentName
        {
            get => _instrumentName;
            set
            {
                if (SetProperty(ref _instrumentName, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        private bool _hasNotes;
        public bool HasNotes
        {
            get => _hasNotes;
            set => SetProperty(ref _hasNotes, value);
        }

        public string DisplayName => $"CH {ChannelNumber:00}  {InstrumentName}";
    }
}
