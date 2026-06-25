using System;

namespace KaraokeApp.Audio
{
    public class PitchDetector
    {
        public float DetectPitch(short[] buffer, int sampleRate)
        {
            int size = buffer.Length;

            int maxLag = sampleRate / 50;   // ~50 Hz
            int minLag = sampleRate / 1000; // ~1000 Hz

            float bestCorrelation = 0;
            int bestLag = 0;

            for (int lag = minLag; lag < maxLag; lag++)
            {
                float sum = 0;

                for (int i = 0; i < size - lag; i++)
                {
                    sum += buffer[i] * buffer[i + lag];
                }

                if (sum > bestCorrelation)
                {
                    bestCorrelation = sum;
                    bestLag = lag;
                }
            }

            if (bestLag == 0)
                return 0;

            return (float)sampleRate / bestLag;
        }
    }
}