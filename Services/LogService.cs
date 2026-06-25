using System;
using System.IO;

namespace KaraokeApp.Services;

public class LogService
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "karaokeapp.log");
    private readonly object _lock = new();

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        var text = message + (ex != null ? $" Exception: {ex}" : string.Empty);
        Write("ERROR", text);
    }

    public void Error(Exception ex)
    {
        Write("ERROR", ex.ToString());
    }

    private void Write(string level, string text)
    {
        try
        {
            lock (_lock)
            {
                var dir = Path.GetDirectoryName(_path) ?? AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(dir);
                File.AppendAllText(_path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {text}\r\n");
            }
        }
        catch
        {
            // non fallire in caso di problemi di logging
        }
    }
}
