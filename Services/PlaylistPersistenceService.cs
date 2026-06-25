using System.Text.Json;
using System;
using System.IO;

namespace KaraokeApp.Services;

public class PlaylistPersistenceService
{
    private readonly string _filePath =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "playlist.json");

    public PlaylistData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new PlaylistData();

            return JsonSerializer.Deserialize<PlaylistData>(
                       File.ReadAllText(_filePath))
                   ?? new PlaylistData();
        }
        catch
        {
            return new PlaylistData();
        }
    }

    public void Save(
        PlaylistData data)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_filePath)!);

        File.WriteAllText(
            _filePath,
            JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }
}