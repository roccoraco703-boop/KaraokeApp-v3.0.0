using KaraokeApp.MVVM;
using System.Collections.ObjectModel;

namespace KaraokeApp.ViewModels
{
    public class LyricLineViewModel : BaseViewModel
    {
        private string _highlightedText = string.Empty;
        private string _pendingText = string.Empty;

        public string Text { get; set; } = string.Empty; // Full line of lyrics
        public ObservableCollection<KaraokeWordViewModel> Words { get; } = new();

        public string HighlightedText
        {
            get => _highlightedText;
            set => SetProperty(ref _highlightedText, value);
        }

        public string PendingText
        {
            get => _pendingText;
            set => SetProperty(ref _pendingText, value);
        }
    }
}
