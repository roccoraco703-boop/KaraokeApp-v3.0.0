using System;
using System.IO;

namespace KaraokeApp.Services;

public static class StartupService
{
    public static void Ensure()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            Directory.CreateDirectory(Path.Combine(baseDir, "Data"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Logs"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Cache"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Songs"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Presets"));
            Directory.CreateDirectory(Path.Combine(baseDir, "VLC"));
            Directory.CreateDirectory(Path.Combine(baseDir, "Docs"));
        }
        catch
        {
            // Do not throw during startup checks
        }
    }
}
