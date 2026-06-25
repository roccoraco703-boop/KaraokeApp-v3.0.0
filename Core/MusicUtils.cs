using System;

namespace KaraokeApp.Core
{
    public static class MusicUtils
    {
        public static double FrequencyToMidi(double frequency)
        {
            if (frequency <= 0)
                return 0;

            return 69 + 12 * Math.Log(frequency / 440.0, 2);
        }

        public static double MidiToY(double midi, double height)
        {
            const double minMidi = 36;
            const double maxMidi = 84;

            double normalized = (midi - minMidi) / (maxMidi - minMidi);
            normalized = Math.Clamp(normalized, 0, 1);

            return height - normalized * height;
        }
    }
}
