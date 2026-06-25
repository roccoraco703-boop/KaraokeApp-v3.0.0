namespace KaraokeApp.Models;

public class LyricWord
{
    public string Text { get; set; } = "";

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public bool Highlighted { get; set; }
}
