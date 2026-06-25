using System;

namespace KaraokeApp.Audio
{
    public static class NoteToFrequencyConverter
    {
        // Converte numero MIDI (0-127) → frequenza Hz
        public static float ToFrequency(int midiNote)
        {
            // Formula standard: A4 = 440Hz, MIDI = 69
            return (float)(440.0 * Math.Pow(2, (midiNote - 69) / 12.0));
        }
    }
}