using NWaves.Transforms;
using System;

namespace KaraokeApp.Audio
{
    public class AutoTuneProcessor
    {
        private readonly int _fftSize = 1024;
        private readonly int _hopSize = 256;

        private readonly Fft _fft;
        private readonly float[] _window;

        private readonly float[] _prevPhase;
        private readonly float[] _sumPhase;
        private readonly float[] _overlap;

        public float TargetFrequency { get; set; }
        public float CurrentPitch { get; set; }

        public AutoTuneProcessor()
        {
            _fft = new Fft(_fftSize);
            _window = CreateHannWindow(_fftSize);

            _prevPhase = new float[_fftSize];
            _sumPhase = new float[_fftSize];
            _overlap = new float[_fftSize];
        }

        public byte[] Process(byte[] buffer, int sampleRate, int channels)
        {
            if (buffer.Length == 0 || sampleRate <= 0 || channels <= 0)
                return buffer;

            float[] samples = BytesToFloat(buffer, channels);
            float[] processed = Process(samples, sampleRate);
            return FloatToBytes(processed);
        }

        public float[] Process(float[] input, int sampleRate)
        {
            float[] real = new float[_fftSize];
            float[] imag = new float[_fftSize];
            float[] shiftedReal = new float[_fftSize];
            float[] shiftedImag = new float[_fftSize];

            Array.Copy(input, real, Math.Min(input.Length, _fftSize));

            for (int i = 0; i < _fftSize; i++)
                real[i] *= _window[i];

            _fft.Direct(real, imag);

            float shift = GetPitchShiftFactor();

            for (int i = 0; i < _fftSize; i++)
            {
                float magnitude = MathF.Sqrt(real[i] * real[i] + imag[i] * imag[i]);
                float phase = MathF.Atan2(imag[i], real[i]);

                float delta = phase - _prevPhase[i];
                _prevPhase[i] = phase;
                _sumPhase[i] += delta;

                int newIndex = (int)(i * shift);
                if (newIndex < _fftSize)
                {
                    shiftedReal[newIndex] += magnitude * MathF.Cos(_sumPhase[i]);
                    shiftedImag[newIndex] += magnitude * MathF.Sin(_sumPhase[i]);
                }
            }

            _fft.Inverse(shiftedReal, shiftedImag);

            float[] output = new float[input.Length];
            int count = Math.Min(input.Length, _hopSize);

            for (int i = 0; i < _fftSize; i++)
            {
                shiftedReal[i] = shiftedReal[i] / _fftSize * _window[i];
                shiftedReal[i] += _overlap[i];
            }

            Array.Copy(shiftedReal, output, count);

            Array.Clear(_overlap, 0, _overlap.Length);
            int overlapCount = _fftSize - _hopSize;
            Array.Copy(shiftedReal, _hopSize, _overlap, 0, overlapCount);

            return output;
        }

        private float GetPitchShiftFactor()
        {
            if (TargetFrequency <= 0 || CurrentPitch <= 0)
                return 1.0f;

            return TargetFrequency / CurrentPitch;
        }

        private static float[] CreateHannWindow(int size)
        {
            float[] window = new float[size];

            for (int i = 0; i < size; i++)
                window[i] = 0.5f - 0.5f * MathF.Cos(2 * MathF.PI * i / (size - 1));

            return window;
        }

        private static float[] BytesToFloat(byte[] buffer, int channels)
        {
            int totalSamples = buffer.Length / 2 / channels;
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                int sourceIndex = i * channels * 2;
                short sample = BitConverter.ToInt16(buffer, sourceIndex);
                samples[i] = sample / 32768f;
            }

            return samples;
        }

        private static byte[] FloatToBytes(float[] samples)
        {
            byte[] buffer = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = Math.Clamp(samples[i], -1f, 1f);
                short sample = (short)(clamped * short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(sample);
                buffer[i * 2] = bytes[0];
                buffer[i * 2 + 1] = bytes[1];
            }

            return buffer;
        }
    }
}
