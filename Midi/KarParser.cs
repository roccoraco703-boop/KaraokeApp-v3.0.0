using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;

namespace KaraokeApp.Midi
{
    public class KarParser
    {
        public List<LyricEvent> Parse(string filePath)
        {
            var midiFile = MidiFile.Read(filePath);
            var tempoMap = midiFile.GetTempoMap();

            var result = new List<LyricEvent>();

            foreach (var track in midiFile.GetTrackChunks())
            {
                var timedEvents = track.GetTimedEvents();
                
                foreach (var timedEvent in timedEvents)
                {
                    string? text = timedEvent.Event switch
                    {
                        TextEvent textEvent => textEvent.Text,
                        Melanchall.DryWetMidi.Core.LyricEvent lyricEvent => lyricEvent.Text,
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var time = TimeConverter.ConvertTo<MetricTimeSpan>(timedEvent.Time, tempoMap);

                        result.Add(new LyricEvent
                        {
                            Text = text,
                            Time = (long)Math.Round(time.TotalMilliseconds)
                        });
                    }
                }
            }

            return NormalizeLyrics(result.OrderBy(x => x.Time).ToList());
        }

        private static List<LyricEvent> NormalizeLyrics(List<LyricEvent> lyricEvents)
        {
            var normalized = new List<LyricEvent>();
            string buffer = string.Empty;
            long phraseStartTime = 0;

            for (int i = 0; i < lyricEvents.Count; i++)
            {
                var lyricEvent = lyricEvents[i];
                string text = lyricEvent.Text ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                bool startsNewPhrase = IsStartOfPhrase(text);
                bool endsPhrase = IsEndOfPhrase(text) || HasLongPauseAfter(lyricEvents, i);

                text = text
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .TrimStart('/', '\\')
                    .Trim();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (startsNewPhrase && !string.IsNullOrWhiteSpace(buffer))
                {
                    normalized.Add(new LyricEvent
                    {
                        Text = "/" + buffer.Trim(),
                        Time = phraseStartTime
                    });

                    buffer = string.Empty;
                }

                if (string.IsNullOrWhiteSpace(buffer))
                    phraseStartTime = lyricEvent.Time;

                if (text.EndsWith("-"))
                    buffer += text.TrimEnd('-');
                else
                    buffer += text + " ";

                if (endsPhrase)
                {
                    normalized.Add(new LyricEvent
                    {
                        Text = "/" + buffer.Trim(),
                        Time = phraseStartTime
                    });

                    buffer = string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(buffer))
            {
                normalized.Add(new LyricEvent
                {
                    Text = "/" + buffer.Trim(),
                    Time = phraseStartTime
                });
            }

            return normalized;
        }

        private static bool IsStartOfPhrase(string text)
        {
            return text.StartsWith("/") || text.StartsWith("\\");
        }

        private static bool IsEndOfPhrase(string text)
        {
            return text.Contains('\n') || text.Contains('\r');
        }

        private static bool HasLongPauseAfter(List<LyricEvent> lyricEvents, int currentIndex)
        {
            if (currentIndex + 1 >= lyricEvents.Count)
                return false;

            return lyricEvents[currentIndex + 1].Time - lyricEvents[currentIndex].Time > 500;
        }
    }
}