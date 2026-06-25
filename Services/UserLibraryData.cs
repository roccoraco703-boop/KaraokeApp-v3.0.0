namespace KaraokeApp.Services;

public class UserLibraryData
{
    public List<string> Favorites { get; set; }
        = new();

    public Dictionary<string, int> PlayCount
        { get; set; }
        = new();

    public Dictionary<string, DateTime>
        LastPlayed
        { get; set; }
        = new();

public List<string> PlayHistory { get; set; } = new();
}