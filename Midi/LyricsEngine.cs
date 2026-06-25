using KaraokeApp.Models;

namespace KaraokeApp.Midi;

public class LyricsEngine
{
    private readonly List<LyricPhrase> _phrases;

    private int _currentPhraseIndex;

    public event Action<LyricPhrase>? PhraseChanged;

    public event Action<LyricWord>? WordChanged;

    public LyricsEngine(
        List<LyricPhrase> phrases)
    {
        _phrases = phrases;
    }

    public void Reset()
    {
        _currentPhraseIndex = 0;
    }

    public void Update(
        long currentTime)
    {
        UpdatePhrase(currentTime);

        UpdateWord(currentTime);
    }

    private void UpdatePhrase(
        long currentTime)
    {
        if (_currentPhraseIndex >=
            _phrases.Count)
            return;

        var phrase =
            _phrases[_currentPhraseIndex];

        if (currentTime >= phrase.StartTime)
        {
            PhraseChanged?.Invoke(
                phrase);

            _currentPhraseIndex++;
        }
    }

    private void UpdateWord(
        long currentTime)
    {
        foreach (var phrase in _phrases)
        {
            foreach (var word in phrase.Words)
            {
                if (currentTime >= word.StartTime &&
                    currentTime <= word.EndTime)
                {
                    WordChanged?.Invoke(word);

                    return;
                }
            }
        }
    }
}