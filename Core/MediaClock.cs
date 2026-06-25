using System;
using System.Diagnostics;
using System.Timers;

namespace KaraokeApp.Core
{
    public class MediaClock
    {
        private Stopwatch? _stopwatch;
        private System.Timers.Timer? _timer;

        public event Action<long>? OnTick;

        public void Start()
        {
            if (_stopwatch == null)
                _stopwatch = new Stopwatch();

            _stopwatch.Restart();

            if (_timer == null)
            {
                _timer = new System.Timers.Timer(16) { AutoReset = true };
                _timer.Elapsed += (s, e) => OnTick?.Invoke(GetMilliseconds());
            }

            _timer.Start();
        }

        public void Pause()
        {
            _timer?.Stop();
            _stopwatch?.Stop();
        }

        public void Resume()
        {
            if (_stopwatch == null)
                _stopwatch = new Stopwatch();

            _stopwatch.Start();
            _timer?.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _stopwatch?.Stop();
        }

        public void Reset()
        {
            _timer?.Stop();
            if (_stopwatch != null)
            {
                _stopwatch.Reset();
            }
        }

        public long GetMilliseconds()
        {
            return _stopwatch?.ElapsedMilliseconds ?? 0;
        }
    }
}