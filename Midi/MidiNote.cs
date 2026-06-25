namespace KaraokeApp.Midi
{
    public class MidiNote
    {
        public int NoteNumber { get; set; }   // 0–127
        public double StartTime { get; set; } // ms
        public double Duration { get; set; }  // ms

        public double Frequency
        {
            get
            {
                return 440.0 * System.Math.Pow(2, (NoteNumber - 69) / 12.0);
            }
        }
    }
}