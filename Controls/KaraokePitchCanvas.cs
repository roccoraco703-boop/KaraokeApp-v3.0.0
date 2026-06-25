
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using KaraokeApp.Core;

namespace KaraokeApp.Controls
{
    public class KaraokePitchCanvas : FrameworkElement
    {
        private readonly List<Point> _voiceTrail = new List<Point>();
        private bool _isPanning = false;
        private Point _lastPanPoint;
        private double _panStartOffsetSeconds;

        public static readonly DependencyProperty NotesProperty =
            DependencyProperty.Register(nameof(Notes), typeof(List<(double Time, double Frequency)>), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(new List<(double Time, double Frequency)>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrentTimeProperty =
            DependencyProperty.Register(nameof(CurrentTime), typeof(double), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrentPitchProperty =
            DependencyProperty.Register(nameof(CurrentPitch), typeof(double), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PixelsPerSecondProperty =
            DependencyProperty.Register(nameof(PixelsPerSecond), typeof(double), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(70.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PanOffsetSecondsProperty =
            DependencyProperty.Register(nameof(PanOffsetSeconds), typeof(double), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SnapToActiveNoteProperty =
            DependencyProperty.Register(nameof(SnapToActiveNote), typeof(bool), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FollowPlaybackProperty =
            DependencyProperty.Register(nameof(FollowPlayback), typeof(bool), typeof(KaraokePitchCanvas), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public List<(double Time, double Frequency)> Notes
        {
            get => (List<(double Time, double Frequency)>)GetValue(NotesProperty);
            set => SetValue(NotesProperty, value);
        }

        public double CurrentTime
        {
            get => (double)GetValue(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public double CurrentPitch
        {
            get => (double)GetValue(CurrentPitchProperty);
            set => SetValue(CurrentPitchProperty, value);
        }

        public double PixelsPerSecond
        {
            get => (double)GetValue(PixelsPerSecondProperty);
            set => SetValue(PixelsPerSecondProperty, value);
        }

        /// <summary>
        /// Positive values shift the timeline to the right (older), negative to the left.
        /// Measured in seconds.
        /// </summary>
        public double PanOffsetSeconds
        {
            get => (double)GetValue(PanOffsetSecondsProperty);
            set => SetValue(PanOffsetSecondsProperty, value);
        }

        public bool SnapToActiveNote
        {
            get => (bool)GetValue(SnapToActiveNoteProperty);
            set => SetValue(SnapToActiveNoteProperty, value);
        }

        public bool FollowPlayback
        {
            get => (bool)GetValue(FollowPlaybackProperty);
            set => SetValue(FollowPlaybackProperty, value);
        }

        public KaraokePitchCanvas()
        {
            Focusable = true;
            this.MouseLeftButtonDown += KaraokePitchCanvas_MouseLeftButtonDown;
            this.MouseMove += KaraokePitchCanvas_MouseMove;
            this.MouseLeftButtonUp += KaraokePitchCanvas_MouseLeftButtonUp;
            this.MouseWheel += KaraokePitchCanvas_MouseWheel;
            this.Cursor = Cursors.Arrow;
        }

        private void KaraokePitchCanvas_MouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(this);
                _panStartOffsetSeconds = PanOffsetSeconds;
                CaptureMouse();
                Cursor = Cursors.Hand;
                e.Handled = true;
            }
        }

        private void KaraokePitchCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                double dx = pos.X - _lastPanPoint.X;
                // dragging to right should move timeline earlier => increase PanOffsetSeconds negatively
                double deltaSeconds = -dx / Math.Max(1.0, PixelsPerSecond);
                PanOffsetSeconds = _panStartOffsetSeconds + deltaSeconds;
                InvalidateVisual();
                e.Handled = true;
            }
        }

        private void KaraokePitchCanvas_MouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void KaraokePitchCanvas_MouseWheel(object? sender, MouseWheelEventArgs e)
        {
            // Optional: zoom with mouse wheel (control zoom), keep gentle limits
            double factor = e.Delta > 0 ? 1.1 : 0.9;
            double newPps = Math.Clamp(PixelsPerSecond * factor, 20.0, 400.0);
            PixelsPerSecond = newPps;
            InvalidateVisual();
            e.Handled = true;
        }

        public void Refresh()
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, width, height)));
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(2, 6, 23)), null, new Rect(0, 0, width, height));

            double pixelsPerSecond = PixelsPerSecond;
            double centerX = width / 2;
            double pan = PanOffsetSeconds; // seconds
            double currentTimeSec = CurrentTime / 1000.0;

            // Follow playback or snap to active note (local override of pan)
            if (SnapToActiveNote && Notes != null && Notes.Count > 0)
            {
                double bestDiff = double.MaxValue;
                double bestNoteTime = 0;
                foreach (var n in Notes)
                {
                    double diff = Math.Abs(n.Time - currentTimeSec);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestNoteTime = n.Time;
                    }
                }

                // only snap if reasonably close (0.8s)
                if (bestDiff <= 0.8)
                {
                    pan = bestNoteTime - currentTimeSec;
                }
            }
            else if (FollowPlayback)
            {
                pan = 0.0; // center current time in view
            }

            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            // draw beat markers (vertical ticks) behind notes
            {
                double beatInterval = 0.5; // seconds per beat marker
                double leftTime = currentTimeSec + pan - (centerX / Math.Max(1.0, pixelsPerSecond));
                double rightTime = currentTimeSec + pan + ((width - centerX) / Math.Max(1.0, pixelsPerSecond));
                double firstBeat = Math.Ceiling(leftTime / beatInterval) * beatInterval;
                var beatPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
                for (double t = firstBeat; t <= rightTime; t += beatInterval)
                {
                    double bx = centerX + ((t - currentTimeSec) - pan) * pixelsPerSecond;
                    if (bx >= -2 && bx <= width + 2)
                        dc.DrawLine(beatPen, new Point(bx, 0), new Point(bx, height));
                }
            }

            // draw horizontal note lines and labels
            for (int i = 40; i <= 80; i++)
            {
                double y = MusicUtils.MidiToY(i, height);
                string name = noteNames[i % 12];

                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(80, 120, 120, 120)), 0.6), new Point(0, y), new Point(width, y));
                dc.DrawText(new FormattedText(name, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 10, Brushes.Gray, 1.25), new Point(4, y - 10));
            }

            double voiceMidi = MusicUtils.FrequencyToMidi(CurrentPitch);
            double activeDiff = double.MaxValue;
            bool hasVoice = CurrentPitch > 0 && !double.IsNaN(voiceMidi) && !double.IsInfinity(voiceMidi);

            // 🎹 DISEGNA NOTE MIDI con pan & zoom
            if (Notes != null)
            {
                foreach (var note in Notes)
                {
                    double noteTimeSec = note.Time; // assumed already in seconds (source uses seconds)
                    double x = centerX + ((noteTimeSec - CurrentTime / 1000.0) - pan) * pixelsPerSecond;
                    double noteMidi = MusicUtils.FrequencyToMidi(note.Frequency);
                    double y = MusicUtils.MidiToY(noteMidi, height);

                    if (x < -60 || x > width + 60 || double.IsNaN(y) || double.IsInfinity(y))
                        continue;

                    Rect rect = new Rect(x - 18, Math.Max(0, Math.Min(height - 12, y - 6)), 36, 12);

                    Brush brush;
                    bool isActive = false;

                    if (hasVoice && Math.Abs(noteTimeSec - CurrentTime / 1000.0) < 0.3)
                    {
                        isActive = true;
                        double diff = Math.Abs(MusicUtils.FrequencyToMidi(note.Frequency) - voiceMidi);
                        activeDiff = diff;
                        if (diff < 0.3)
                            brush = Brushes.Lime;
                        else if (diff < 1.0)
                            brush = Brushes.Yellow;
                        else
                            brush = Brushes.Red;
                    }
                    else
                    {
                        // modern karaoke blue for inactive notes
                        brush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    }

                    if (isActive)
                    {
                        Color color = ((SolidColorBrush)brush).Color;
                        Rect glowRect = new Rect(rect.X - 10, rect.Y - 10, rect.Width + 20, rect.Height + 20);
                        SolidColorBrush glowBrush = new SolidColorBrush(Color.FromArgb(140, color.R, color.G, color.B));
                        dc.DrawRectangle(glowBrush, null, glowRect);
                        dc.DrawRectangle(brush, new Pen(brush, 2), rect);
                    }
                    else
                    {
                        dc.DrawRectangle(brush, new Pen(brush, 1), rect);
                    }
                }
            }

            // 🎤 DISEGNA VOCE (LINEA) — considera pan per visualizzazione della traccia come relativa al centro
            if (!hasVoice)
            {
                _voiceTrail.Clear();
                // draw center cursor
                dc.DrawLine(new Pen(Brushes.White, 1.2), new Point(centerX, 0), new Point(centerX, height));
                dc.Pop();
                return;
            }

            double voiceY = MusicUtils.MidiToY(voiceMidi, height);
            Brush voiceBrush = activeDiff < 0.3 ? Brushes.Lime : activeDiff < 1.0 ? Brushes.Yellow : Brushes.Red;

            // add the current voice point at centerX (cursor-centered)
            _voiceTrail.Add(new Point(centerX, voiceY));
            if (_voiceTrail.Count > 120)
                _voiceTrail.RemoveAt(0);

            for (int i = 1; i < _voiceTrail.Count; i++)
            {
                dc.DrawLine(new Pen(voiceBrush, i * 0.4), _voiceTrail[i - 1], _voiceTrail[i]);
            }

            // PROFESSIONAL CENTER CURSOR: vertical band, glow and time label
            // vertical soft band
            var bandBrush = new LinearGradientBrush();
            bandBrush.StartPoint = new Point(0, 0); bandBrush.EndPoint = new Point(1, 0);
            bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.0));
            bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(40, 255, 255, 255), 0.45));
            bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(40, 255, 255, 255), 0.55));
            bandBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1.0));
            dc.DrawRectangle(bandBrush, null, new Rect(centerX - 14, 0, 28, height));

            // subtle glow around center (pulsing based on time)
            double phase = currentTimeSec;
            double pulse = 0.6 + 0.4 * (0.5 + 0.5 * Math.Sin(phase * 2 * Math.PI * 1.8));
            byte alpha = (byte)(80 * pulse);
            var centerGlowBrush = new RadialGradientBrush(Color.FromArgb(alpha, 255, 255, 255), Color.FromArgb(0, 255, 255, 255));
            centerGlowBrush.GradientOrigin = new Point(0.5, 0.5);
            centerGlowBrush.Center = new Point(0.5, 0.5);
            centerGlowBrush.RadiusX = 0.15; centerGlowBrush.RadiusY = 0.9; // elongated glow vertically
            // draw glow as tall ellipse centered at voiceY
            dc.PushTransform(new TranslateTransform(centerX - 60, voiceY - 40));
            dc.DrawEllipse(centerGlowBrush, null, new Point(60, 40), 80, 40);
            dc.Pop();

            // central solid line (stronger for karaoke cursor)
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), 4), new Point(centerX, 0), new Point(centerX, height));

            // marker at voice position
            dc.DrawEllipse(voiceBrush, null, new Point(centerX, voiceY), 5, 5);

            // draw current time label above center
            var timeText = TimeSpan.FromSeconds(currentTimeSec);
            string timeStr = string.Format("{0:D2}:{1:D2}.{2:D3}", timeText.Minutes, timeText.Seconds, timeText.Milliseconds);
            var ft = new FormattedText(timeStr, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.White, 1.25);
            dc.DrawText(ft, new Point(centerX - ft.Width / 2, 4));

            dc.Pop();
        }
    }
}
