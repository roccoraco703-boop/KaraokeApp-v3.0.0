using System;
using System.IO;
using System.Text.Json;

namespace KaraokeApp.Services;

public class UserLibraryService
{
    private readonly string _filePath =
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "userlibrary.json");

    public UserLibraryData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new UserLibraryData();

            return JsonSerializer.Deserialize<UserLibraryData>(
                       File.ReadAllText(_filePath))
                   ?? new UserLibraryData();
        }
        catch
        {
            return new UserLibraryData();
        }
    }

    public void Save(
        UserLibraryData data)
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
