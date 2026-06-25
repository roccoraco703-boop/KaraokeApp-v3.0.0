using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using System;

namespace KaraokeApp.Audio
{
    public class MidiPlaybackService
    {
        private OutputDevice? _outputDevice;
        private Playback? _playback;
        public event Action<string>? OnStatusChanged;

        public void Play(string filePath)
        {
            try
            {
                Stop();

                var midiFile = MidiFile.Read(filePath);

                if (OutputDevice.GetDevicesCount() == 0)
                {
                    OnStatusChanged?.Invoke("Nessun dispositivo MIDI trovato: continuo con lyrics e video");
                    return;
                }
                _outputDevice = OutputDevice.GetByIndex(0);
                _playback = midiFile.GetPlayback(_outputDevice);

                _playback.Start();
                OnStatusChanged?.Invoke("MIDI avviato");
            }
            catch (Exception ex)
            {
                Stop();
                OnStatusChanged?.Invoke("MIDI non disponibile: " + ex.Message);
            }
        }

        // Pause/Resume not implemented for Playback - rely on clock control and Stop/Play

        public void Stop()
        {
            _playback?.Stop();
            _playback?.Dispose();
            _playback = null;
            _outputDevice?.Dispose();
            _outputDevice = null;
        }
    }
}