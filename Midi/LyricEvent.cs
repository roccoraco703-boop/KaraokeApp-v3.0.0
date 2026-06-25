namespace KaraokeApp.Midi
{
    public class LyricEvent
    {
        public string? Text { get; set; } // Represents the full lyric text
        public long Time { get; set; } // Timestamp for synchronization
    }
}