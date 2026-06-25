using System.Windows;
using System.Threading.Tasks;

namespace KaraokeApp.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private async void SplashWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await Task.Delay(1500); // 1.5 seconds
        Close();
    }
}
