namespace KaraokeApp.Services;

public static class AppServices
{
    public static readonly
        UserLibraryService
        UserLibrary =
            new();

    public static readonly
        PlaylistPersistenceService
        Playlist =
            new();

    public static readonly
        LogService
        Log =
            new();
}