using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace KaraokeApp.Midi
{
    public class MidiNoteParser
    {
        public List<MidiNoteEvent> Parse(string filePath)
        {
            var midiFile = MidiFile.Read(filePath);

            var tempoMap = midiFile.GetTempoMap();

            var notes = midiFile.GetNotes();

            var result = new List<MidiNoteEvent>();

            foreach (var note in notes)
            {
                var start = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                var length = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, tempoMap);

                result.Add(new MidiNoteEvent
                {
                    NoteNumber = note.NoteNumber,
                    StartTime = start.TotalMilliseconds,
                    EndTime = start.TotalMilliseconds + length.TotalMilliseconds
                });
            }

            return result;
        }
    }
}