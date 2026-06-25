namespace KaraokeApp.Core
{
    public class ScoreEngine
    {
        private int _score;
        private int _combo;

        public int Score => _score;
        public int Combo => _combo;

        public void Reset()
        {
            _score = 0;
            _combo = 0;
        }

        public void RegisterHit(float accuracy)
        {
            if (accuracy > 80)
            {
                _combo++;
                _score += (int)(10 * (1 + _combo * 0.5));
            }
            else
            {
                _combo = 0;
                _score += (int)(accuracy / 10);
            }
        }
    }
}