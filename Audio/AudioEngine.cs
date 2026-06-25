using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace KaraokeApp.Audio
{
    public class AudioEngine : IDisposable
    {
        private readonly MixingSampleProvider _mixer;
        private WaveOutEvent _output;
        private AudioFileReader? _musicReader;
        private ISampleProvider? _microphoneProvider;
        private VolumeSampleProvider? _microphoneVolumeProvider;
        private BufferedWaveProvider? _microphoneBuffer;

        public PitchCorrectionService PitchCorrection { get; } = new PitchCorrectionService();
        public float MusicVolume { get; set; } = 0.8f;
        private float _microphoneVolume = 1.0f;
        public float MicrophoneVolume
        {
            get => _microphoneVolume;
            set
            {
                _microphoneVolume = Math.Clamp(value, 0f, 2f);

                if (_microphoneVolumeProvider != null)
                    _microphoneVolumeProvider.Volume = IsMicrophoneMuted ? 0f : _microphoneVolume;
            }
        }
        public bool IsMicrophoneMuted { get; private set; }
        public bool IsMixerRunning { get; private set; }

        public event Action<string>? OnStatusChanged;

        public AudioEngine()
        {
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };

            _output = new WaveOutEvent();
            _output.Init(_mixer);
        }

        public IReadOnlyList<string> GetOutputDevices()
        {
            var devices = new List<string> { "Predefinito di Windows" };

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                devices.Add(capabilities.ProductName);
            }

            return devices;
        }

        public void SetOutputDevice(int deviceNumber)
        {
            bool wasRunning = IsMixerRunning;

            try
            {
                _output.Stop();
                _output.Dispose();
                _output = new WaveOutEvent
                {
                    DeviceNumber = deviceNumber
                };
                _output.Init(_mixer);

                if (wasRunning)
                    _output.Play();

                IsMixerRunning = wasRunning;
                OnStatusChanged?.Invoke(deviceNumber < 0 ? "Audio su uscita predefinita di Windows" : "Audio su dispositivo esterno selezionato");
            }
            catch (Exception ex)
            {
                IsMixerRunning = false;
                OnStatusChanged?.Invoke("Errore selezione uscita audio: " + ex.Message);
            }
        }

        public void LoadMusic(string filePath)
        {
            try
            {
                StopMusic();

                _musicReader = new AudioFileReader(filePath)
                {
                    Volume = MusicVolume
                };

                _mixer.AddMixerInput(ConvertToMixerFormat(_musicReader));
                OnStatusChanged?.Invoke("Musica caricata nel mixer");
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Mixer musica non disponibile: " + ex.Message);
            }
        }

        public void AttachMicrophone(ISampleProvider microphoneProvider)
        {
            _microphoneProvider = PitchCorrection.CreateCorrectedMicrophoneProvider(microphoneProvider);
            _microphoneVolumeProvider = new VolumeSampleProvider(_microphoneProvider)
            {
                Volume = IsMicrophoneMuted ? 0f : MicrophoneVolume
            };

            _mixer.AddMixerInput(ConvertToMixerFormat(_microphoneVolumeProvider));
            OnStatusChanged?.Invoke("Microfono collegato al mixer");
        }

        public void AttachMicrophone(WaveFormat waveFormat)
        {
            if (_microphoneBuffer != null)
                return;

            _microphoneBuffer = new BufferedWaveProvider(waveFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(200)
            };

            AttachMicrophone(_microphoneBuffer.ToSampleProvider());
        }

        public void AddMicrophoneSamples(byte[] buffer, WaveFormat waveFormat)
        {
            AttachMicrophone(waveFormat);

            if (_microphoneBuffer == null)
                return;

            _microphoneBuffer.AddSamples(buffer, 0, buffer.Length);
        }

        public void ToggleMicrophoneMute()
        {
            IsMicrophoneMuted = !IsMicrophoneMuted;

            if (_microphoneVolumeProvider != null)
                _microphoneVolumeProvider.Volume = IsMicrophoneMuted ? 0f : MicrophoneVolume;

            OnStatusChanged?.Invoke(IsMicrophoneMuted ? "Monitor microfono muto" : "Monitor microfono attivo");
        }

        public void Start()
        {
            try
            {
                _output.Play();
                IsMixerRunning = true;
                OnStatusChanged?.Invoke("Mixer avviato");
            }
            catch (Exception ex)
            {
                IsMixerRunning = false;
                OnStatusChanged?.Invoke("Audio output non disponibile: " + ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                _output.Stop();
            }
            catch
            {
            }

            IsMixerRunning = false;
            OnStatusChanged?.Invoke("Mixer fermato");
        }

        public void StopMusic()
        {
            _musicReader?.Dispose();
            _musicReader = null;
        }

        public void Dispose()
        {
            Stop();
            StopMusic();
            _output.Dispose();
        }

        private ISampleProvider ConvertToMixerFormat(ISampleProvider source)
        {
            ISampleProvider result = source;

            if (result.WaveFormat.Channels == 1)
                result = new MonoToStereoSampleProvider(result);

            if (result.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
                result = new WdlResamplingSampleProvider(result, _mixer.WaveFormat.SampleRate);

            return result;
        }
    }
}
