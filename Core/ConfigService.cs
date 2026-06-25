using System;
using System.IO;
using System.Text.Json;

namespace KaraokeApp.Core
{
    public class ConfigService
    {
        private readonly string _settingsPath;

        public AppSettings Settings { get; set; } = new AppSettings();

        public ConfigService()
        {
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        }

        public void Load()
        {
            if (!File.Exists(_settingsPath))
            {
                Save();
                return;
            }

            string json = File.ReadAllText(_settingsPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppSettings();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
        }
    }
}
