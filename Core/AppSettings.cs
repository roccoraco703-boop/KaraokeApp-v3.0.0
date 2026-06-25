namespace KaraokeApp.Core
{
    public class AppSettings
    {
        public float MicVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.8f;
        public int OffsetMs { get; set; } = 120;
        public string AutoTuneMode { get; set; } = "Soft";
        public bool StartMixerEnabled { get; set; }
        public int AudioOutputDeviceNumber { get; set; } = -1;
        public int LyricsOffsetMs { get; set; } = 0;
        public int MidiOffsetMs { get; set; } = 0;
        public int VideoOffsetMs { get; set; } = 0;
        public string? LibraryPath { get; set; } = null; // E2: Percorso cartella karaoke
    }
}
