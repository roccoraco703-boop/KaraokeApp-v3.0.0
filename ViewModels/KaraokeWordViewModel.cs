using KaraokeApp.MVVM;

namespace KaraokeApp.ViewModels
{
    public class KaraokeWordViewModel : BaseViewModel
    {
        private bool _isHighlighted;

        private bool _isCurrent;

        public string Text { get; set; } = string.Empty;

        public long Time { get; set; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => SetProperty(ref _isHighlighted, value);
        }

        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }
    }
}
