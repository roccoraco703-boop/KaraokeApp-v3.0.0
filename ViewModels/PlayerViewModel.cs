using KaraokeApp.MVVM;
using KaraokeApp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KaraokeApp.ViewModels
{
    public class PlayerViewModel : BaseViewModel
    {
        public event Action? OnFrame;
        public Func<long>? GetVideoTimeMs;
        public bool HasVideoSource { get; set; }

        public ICommand? StartCommand { get; set; }
        public ICommand? StopCommand { get; set; }
        public ICommand? BackCommand { get; set; }

        private bool _isYouTube;
        public bool IsYouTube
        {
            get => _isYouTube;
            set => SetProperty(ref _isYouTube, value);
        }

        private string _youTubeUrl = string.Empty;
        public string YouTubeUrl
        {
            get => _youTubeUrl;
            set => SetProperty(ref _youTubeUrl, value);
        }

        private string _lyrics = string.Empty;
        public string Lyrics
        {
            get => _lyrics;
            set => SetProperty(ref _lyrics, value);
        }

        private string _highlightedLyrics = string.Empty;
        public string HighlightedLyrics
        {
            get => _highlightedLyrics;
            set => SetProperty(ref _highlightedLyrics, value);
        }

        private string _pendingLyrics = string.Empty;
        public string PendingLyrics
        {
            get => _pendingLyrics;
            set => SetProperty(ref _pendingLyrics, value);
        }

        private string _currentPhrase = "";

        public string CurrentPhrase
        {
            get => _currentPhrase;
            set => SetProperty(
                ref _currentPhrase,
                value);
        }

        private string _nextPhrase = "";

        public string NextPhrase
        {
            get => _nextPhrase;
            set => SetProperty(
                ref _nextPhrase,
                value);
        }

        private string _currentWord = "";

        public string CurrentWord
        {
            get => _currentWord;
            set => SetProperty(
                ref _currentWord,
                value);
        }

        public ObservableCollection<LyricLineViewModel> LyricLines { get; } = new();

                // Playlist for the player - each item holds a Song and position
                public ObservableCollection<PlaylistItemViewModel> Playlist { get; } = new();

                private PlaylistItemViewModel? _selectedPlaylistItem;
                public PlaylistItemViewModel? SelectedPlaylistItem
                {
                    get => _selectedPlaylistItem;
                    set => SetProperty(ref _selectedPlaylistItem, value);
                }

        public ObservableCollection<KaraokeWordViewModel> CurrentLineWords { get; } = new();

        private string _topLine = "";

        public string TopLine
        {
            get => _topLine;
            set => SetProperty(ref _topLine, value);
        }

        private string _bottomLine = "";

        public string BottomLine
        {
            get => _bottomLine;
            set => SetProperty(ref _bottomLine, value);
        }

        private LyricLineViewModel? _currentLyricLine;
        public LyricLineViewModel? CurrentLyricLine
        {
            get => _currentLyricLine;
            set => SetProperty(ref _currentLyricLine, value);
        }

        private int _currentWordIndex;
        public int CurrentWordIndex
        {
            get => _currentWordIndex;
            set
            {
                if (SetProperty(ref _currentWordIndex, value))
                    UpdateHighlightedLyrics();
            }
        }

        private string _note = string.Empty;
        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        private string _videoPath = string.Empty;
        public string VideoPath
        {
            get => _videoPath;
            set => SetProperty(ref _videoPath, value);
        }

        private List<(double Time, double Frequency)> _pitchNotes = new List<(double Time, double Frequency)>();
        public List<(double Time, double Frequency)> PitchNotes
        {
            get => _pitchNotes;
            set => SetProperty(ref _pitchNotes, value);
        }

        private double _currentTime;
        public double CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private long _currentTimeMs;

        public long CurrentTimeMs
        {
            get => _currentTimeMs;
            set => SetProperty(
                ref _currentTimeMs,
                value);
        }

        private double _currentPitch;
        public double CurrentPitch
        {
            get => _currentPitch;
            set => SetProperty(ref _currentPitch, value);
        }

        private int _combo;
        public int Combo
        {
            get => _combo;
            set => SetProperty(ref _combo, value);
        }

        private int _score;
        public int Score
        {
            get => _score;
            set
            {
                if (SetProperty(ref _score, value))
                    OnPropertyChanged(nameof(Rank));
            }
        }

        private int _maxScore = 1000;
        public int MaxScore
        {
            get => _maxScore;
            set
            {
                if (SetProperty(ref _maxScore, value))
                    OnPropertyChanged(nameof(Rank));
            }
        }

        private bool _isFinished;
        public bool IsFinished
        {
            get => _isFinished;
            set => SetProperty(ref _isFinished, value);
        }

        public string Rank
        {
            get
            {
                double perc = MaxScore > 0 ? (double)Score / MaxScore : 0;

                if (perc > 0.9) return "S";
                if (perc > 0.75) return "A";
                if (perc > 0.6) return "B";
                if (perc > 0.4) return "C";
                return "D";
            }
        }

        public double ApplyPitchCorrection(double voice, double target)
        {
            double corrected = SnapToTarget(voice, target);
            CurrentPitch = corrected;
            return corrected;
        }

        private double SnapToTarget(double voice, double target)
        {
            double vMidi = MusicUtils.FrequencyToMidi(voice);
            double tMidi = MusicUtils.FrequencyToMidi(target);

            if (Math.Abs(vMidi - tMidi) < 0.5)
                return target;

            return voice;
        }

        public void RaiseFrame()
        {
            OnFrame?.Invoke();
        }

        private void UpdateHighlightedLyrics()
        {
            for (int i = 0; i < CurrentLineWords.Count; i++)
            {
                var word = CurrentLineWords[i];

                word.IsCurrent = i == CurrentWordIndex;

                word.IsHighlighted = i < CurrentWordIndex;
            }
        }
    }
}
