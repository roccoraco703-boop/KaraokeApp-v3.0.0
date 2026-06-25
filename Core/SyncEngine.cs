using KaraokeApp.Midi;
using KaraokeApp.Audio;
using KaraokeApp.Models;
using System;
using System.Collections.Generic;

namespace KaraokeApp.Core
{
    public class SyncEngine
    {
        private readonly MediaClock _clock;

        // 🎤 LYRICS
        private List<LyricPhrase>? _lyricLines;

        private int _currentLineIndex = -1;
        private int _currentWordIndex = -1;

        // 🎹 NOTE MIDI
        private List<MidiNoteEvent>? _notes;
        private int _noteIndex;

        private int _micOffset = 0;

        // 📢 EVENTS
        public event Action<KaraokeApp.Models.LyricPhrase>? OnCurrentPhraseChanged;
        public event Action<KaraokeApp.Models.LyricPhrase>? OnNextPhraseChanged;
        public event Action<int>? OnCurrentWordChanged;
        public event Action<long>? OnTick;
        public event Action<double>? OnNoteChanged;

        public SyncEngine(MediaClock clock)
        {
            _clock = clock;
        }

        public int LyricsOffsetMs { get; set; }

        public int MidiOffsetMs { get; set; }

        // 🎤 LOAD LYRICS
        public void LoadLyrics(List<LyricEvent> lyrics)
        {
            // Convert LyricEvent list (timed events) into LyricPhrase list with words and approximate timings
            var phrases = new List<LyricPhrase>();

            for (int i = 0; i < lyrics.Count; i++)
            {
                var ev = lyrics[i];
                long start = ev.Time;
                long end = (i + 1 < lyrics.Count) ? lyrics[i + 1].Time - 1 : start + 3000;
                string text = (ev.Text ?? string.Empty).TrimStart('/').Trim();

                var phrase = new LyricPhrase
                {
                    Text = text,
                    StartTime = start,
                    EndTime = end
                };

                var words = string.IsNullOrWhiteSpace(text)
                    ? Array.Empty<string>()
                    : text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length > 0)
                {
                    long totalSpan = Math.Max(1, end - start);
                    for (int w = 0; w < words.Length; w++)
                    {
                        long wStart = start + (totalSpan * w) / words.Length;
                        long wEnd = start + (totalSpan * (w + 1)) / words.Length - 1;
                        phrase.Words.Add(new LyricWord
                        {
                            Text = words[w],
                            StartTime = wStart,
                            EndTime = wEnd
                        });
                    }
                }

                phrases.Add(phrase);
            }

            _lyricLines = phrases;
            _currentLineIndex = -1;
            _currentWordIndex = -1;
        }

        // 🎹 LOAD NOTES
        public void LoadNotes(List<MidiNoteEvent> notes)
        {
            _notes = notes;
            _noteIndex = 0;
        }

        public void SetVideoOffset(int ms)
        {
            // compatibilità legacy
        }

        public void SetMicOffset(int ms)
        {
            _micOffset = ms;
        }

        // 🔄 UPDATE LOOP
        public void Update()
        {
            Update(_clock.GetMilliseconds());
        }

        public void Update(long externalTime)
        {
            long currentTime = externalTime;

            OnTick?.Invoke(currentTime);

            if (_lyricLines != null)
            {
                UpdateLyrics(
                    currentTime + LyricsOffsetMs);
            }

            UpdateMidiNotes(
                currentTime + MidiOffsetMs);

            UpdateScore(currentTime);
        }

        private void UpdateMidiNotes(long currentTime)
        {
            // 🎹 NOTE MIDI
            while (_notes != null && _noteIndex < _notes.Count && currentTime >= _notes[_noteIndex].StartTime)
            {
                OnNoteChanged?.Invoke(NoteToFrequencyConverter.ToFrequency(_notes[_noteIndex].NoteNumber));
                _noteIndex++;
            }
        }

        private void UpdateScore(long currentTime)
        {
            // Placeholder for score-related timing updates if needed in future
        }

        private void UpdateLyrics(long currentTime)
        {
            if (_lyricLines == null)
                return;

            for (int lineIndex = 0; lineIndex < _lyricLines.Count; lineIndex++)
            {
                var line = _lyricLines[lineIndex];

                if (currentTime < line.StartTime || currentTime > line.EndTime)
                    continue;

                if (_currentLineIndex != lineIndex)
                {
                    _currentLineIndex = lineIndex;

                    OnCurrentPhraseChanged?.Invoke(line);

                    var nextPhrase = lineIndex + 1 < _lyricLines.Count ? _lyricLines[lineIndex + 1] : null;

                    if (nextPhrase != null)
                        OnNextPhraseChanged?.Invoke(nextPhrase);
                    else
                        OnNextPhraseChanged?.Invoke(new LyricPhrase { Text = string.Empty, StartTime = 0, EndTime = 0 });
                }

                for (int wordIndex = 0; wordIndex < line.Words.Count; wordIndex++)
                {
                    var word = line.Words[wordIndex];

                    if (currentTime >= word.StartTime && currentTime <= word.EndTime)
                    {
                        if (_currentWordIndex != wordIndex)
                        {
                            _currentWordIndex = wordIndex;

                            OnCurrentWordChanged?.Invoke(wordIndex);
                        }

                        return;
                    }
                }
            }
        }
    }
}