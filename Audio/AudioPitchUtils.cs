using System;

namespace KaraokeApp.Audio
{
    public static class AudioPitchUtils
    {
        public static string FrequencyToNote(float frequency)
        {
            if (frequency <= 0) return "-";

            string[] notes =
            {
                "C","C#","D","D#","E","F","F#","G","G#","A","A#","B"
            };

            int noteIndex = (int)(12 * (Math.Log(frequency / 440.0) / Math.Log(2)) + 69);

            int note = noteIndex % 12;
            int octave = noteIndex / 12 - 1;

            return $"{notes[note]}{octave}";
        }

        public static float CentsDifference(float freq1, float freq2)
        {
            if (freq1 <= 0 || freq2 <= 0) return 0;

            return 1200f * (float)Math.Log(freq1 / freq2, 2);
        }

        public static float GetAccuracy(float detectedFreq, float targetFreq)
        {
            if (detectedFreq <= 0 || targetFreq <= 0)
                return 0;

            float cents = Math.Abs(CentsDifference(detectedFreq, targetFreq));

            float accuracy = Math.Max(0, 100 - cents);

            return Math.Clamp(accuracy, 0, 100);
        }
    }
}