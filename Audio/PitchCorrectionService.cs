using KaraokeApp.Core;
using NAudio.Wave;
using System;

namespace KaraokeApp.Audio
{
    public class PitchCorrectionService
    {
        public bool IsEnabled { get; set; }
        public double Strength { get; set; } = 0.35;
        public double SnapThresholdSemitones { get; set; } = 0.6;
        public AutoTuneProcessor Processor { get; } = new AutoTuneProcessor();

        public double CorrectPitch(double voiceFrequency, double targetFrequency)
        {
            if (!IsEnabled || voiceFrequency <= 0 || targetFrequency <= 0)
                return voiceFrequency;

            double voiceMidi = MusicUtils.FrequencyToMidi(voiceFrequency);
            double targetMidi = MusicUtils.FrequencyToMidi(targetFrequency);
            double diff = targetMidi - voiceMidi;

            if (Math.Abs(diff) > SnapThresholdSemitones)
                return voiceFrequency;

            double correctedMidi = voiceMidi + diff * Strength;
            return 440.0 * Math.Pow(2, (correctedMidi - 69.0) / 12.0);
        }

        public ISampleProvider CreateCorrectedMicrophoneProvider(ISampleProvider microphoneProvider)
        {
            return microphoneProvider;
        }

        public byte[] ProcessPcm16(byte[] buffer, int sampleRate, int channels, double targetFrequency)
        {
            if (!IsEnabled)
                return buffer;

            Processor.TargetFrequency = (float)targetFrequency;
            return Processor.Process(buffer, sampleRate, channels);
        }
    }
}
