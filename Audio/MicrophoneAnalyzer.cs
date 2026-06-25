using NAudio.Wave;
using NAudio.CoreAudioApi;
using System;
using System.Linq;

namespace KaraokeApp.Audio
{
    public class MicrophoneAnalyzer
    {
        private WasapiCapture? _capture;
        private readonly PitchDetector _detector = new PitchDetector();

        public event Action<float>? OnPitchDetected;
        public event Action<string>? OnStatusChanged;
        public event Action<byte[], WaveFormat>? OnAudioAvailable;

        public void Start()
        {
            Stop();

            ReportDevices();

            var device = FindPreferredInputDevice();

            if (device == null)
            {
                OnStatusChanged?.Invoke("Nessun microfono trovato");
                return;
            }

            OnStatusChanged?.Invoke("Microfono: " + device.FriendlyName);

            _capture = new WasapiCapture(device, false, 20);

            try
            {
                _capture.DataAvailable += OnData;
                _capture.StartRecording();
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Errore microfono: " + ex.Message);
                Stop();
            }
        }

        public void Stop()
        {
            _capture?.StopRecording();
            _capture?.Dispose();
            _capture = null;
        }

        private MMDevice? FindPreferredInputDevice()
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            return devices.FirstOrDefault(d => d.FriendlyName.Contains("USB", StringComparison.OrdinalIgnoreCase)) ??
                   devices.FirstOrDefault(d => d.FriendlyName.Contains("Mic", StringComparison.OrdinalIgnoreCase)) ??
                   devices.FirstOrDefault(d => d.FriendlyName.Contains("Microphone", StringComparison.OrdinalIgnoreCase)) ??
                   devices.FirstOrDefault(d => d.FriendlyName.Contains("Microfono", StringComparison.OrdinalIgnoreCase)) ??
                   devices.FirstOrDefault();
        }

        private void ReportDevices()
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            if (devices.Count == 0)
            {
                OnStatusChanged?.Invoke("Windows non vede dispositivi input");
                return;
            }

            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"MIC {i}: {devices[i].FriendlyName}");
            }
        }

        private void OnData(object? sender, WaveInEventArgs e)
        {
            if (_capture != null)
            {
                byte[] audio = new byte[e.BytesRecorded];
                Buffer.BlockCopy(e.Buffer, 0, audio, 0, e.BytesRecorded);
                OnAudioAvailable?.Invoke(audio, _capture.WaveFormat);
            }

            int bytesPerSample = _capture?.WaveFormat.BitsPerSample / 8 ?? 2;
            int channels = _capture?.WaveFormat.Channels ?? 1;
            int samples = e.BytesRecorded / bytesPerSample / channels;
            short[] buffer = new short[samples];

            for (int i = 0; i < samples; i++)
            {
                int sourceIndex = i * channels * bytesPerSample;

                if (bytesPerSample == 4)
                {
                    float sample = BitConverter.ToSingle(e.Buffer, sourceIndex);
                    buffer[i] = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                }
                else
                {
                    buffer[i] = BitConverter.ToInt16(e.Buffer, sourceIndex);
                }
            }

            int sampleRate = _capture?.WaveFormat.SampleRate ?? 44100;
            float pitch = _detector.DetectPitch(buffer, sampleRate);

            if (pitch > 50 && pitch < 1000)
                OnPitchDetected?.Invoke(pitch);
        }
    }
}