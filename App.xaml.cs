using System.Windows;
using System;
using System.IO;
using System.Windows.Threading;

namespace KaraokeApp
{
    public partial class App : Application
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "karaokeapp-error.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // ensure required folders exist
            KaraokeApp.Services.StartupService.Ensure();

            // show splash
            var splash = new Views.SplashWindow();
            splash.Show();

            // load main window while splash is visible
            var mainWindow = new MainWindow();
            mainWindow.Opacity = 0; // hide until splash closes
            mainWindow.Show();

            // when splash closes, reveal main window
            splash.Closed += (s, args) =>
            {
                mainWindow.Opacity = 1;
                mainWindow.Activate();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // eventuali save finali
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show("Si è verificato un errore imprevisto. Il dettaglio è stato salvato nel file karaokeapp-error.log.", "KaraokeApp", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
                LogException(exception);
        }

        private static void LogException(Exception exception)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {exception}\r\n\r\n");
        }
    }
}

