namespace KaraokeApp.Midi
{
    public class MidiNoteEvent
    {
        public int NoteNumber { get; set; }   // 0-127
        public double StartTime { get; set; } // ms
        public double EndTime { get; set; }   // ms
    }
}