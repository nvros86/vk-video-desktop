using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel { get; }

    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadRecommendationsAsync();
    }

    private void OnVideoItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private void OnHistoryItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }
}
