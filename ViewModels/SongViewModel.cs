namespace KaraokeApp.ViewModels
{
    public class SongViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string MidiPath { get; set; } = string.Empty;
        public string KarPath { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
        public string YouTubeUrl { get; set; } = string.Empty;
        public bool UseYouTube { get; set; }
        public bool IsCached { get; set; }
        public string CoverPath { get; set; } = string.Empty;
        public int OffsetMs { get; set; }
        public bool MidiExists { get; set; }
        public bool KarExists { get; set; }
        public bool VideoExists { get; set; }
        public bool HasYouTube => !string.IsNullOrWhiteSpace(YouTubeUrl);
        public bool IsPlayable => MidiExists && KarExists && (VideoExists || HasYouTube || string.IsNullOrWhiteSpace(VideoPath));
        public bool HasAllFiles { get; set; }

        public string Status
        {
            get
            {
                if (HasAllFiles)
                    return "READY";

                return "MISSING";
            }
        }
        public string Artist { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public int PlayCount { get; set; }

        public DateTime? LastPlayed { get; set; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Artist))
                    return Title;

                return $"{Artist} - {Title}";
            }
        }

        public string GetVideoSource()
        {
            return UseYouTube ? "YouTube" : "Local";
        }
    }
}
