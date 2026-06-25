namespace KaraokeApp.Models;

public class LyricPhrase
{
    public string Text { get; set; } = "";

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public List<LyricWord> Words { get; set; }
        = new();
}