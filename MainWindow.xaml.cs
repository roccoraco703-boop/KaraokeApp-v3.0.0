using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Wpf;
using KaraokeApp.ViewModels;

namespace KaraokeApp
{
    public partial class MainWindow : Window
    {
        private MediaElement? _videoPlayer;
        private WebView2? _youTubePlayer;
        private FrameworkElement? _videoPlaceholder;

        public MainWindow()
        {
            InitializeComponent();

            // Imposta il DataContext con MainViewModel
            DataContext = new MainViewModel();

            Services.AppServices.Log.Info("MainWindow: DataContext set to MainViewModel");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoPlayer = (MediaElement?)FindName("VideoPlayer");
                _youTubePlayer = (WebView2?)FindName("YouTubePlayer");
                _videoPlaceholder = (FrameworkElement?)FindName("VideoPlaceholder");

                Services.AppServices.Log.Info("MainWindow_Loaded: video elements resolved");
            }
            catch (Exception ex)
            {
                Services.AppServices.Log.Error("MainWindow_Loaded error", ex);
            }

            if (DataContext is MainViewModel app)
            {
                Services.AppServices.Log.Info("MainWindow_Loaded: DataContext is MainViewModel");
                if (_videoPlayer != null)
                {
                    app.PlayerVM.GetVideoTimeMs = () =>
                        _videoPlayer.Source != null
                            ? (long)_videoPlayer.Position.TotalMilliseconds
                            : 0;
                }
            }
            else
            {
                Services.AppServices.Log.Error("MainWindow_Loaded: DataContext is NOT MainViewModel");
            }
        }
    }
}

