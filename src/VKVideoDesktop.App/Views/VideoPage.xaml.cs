using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class VideoPage : Page
{
    public VideoViewModel ViewModel { get; }

    public VideoPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<VideoViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string videoId)
        {
            await ViewModel.LoadVideoAsync(videoId);
        }
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        // TODO: Implement video player
    }

    private void OnFullscreenClick(object sender, RoutedEventArgs e)
    {
        // TODO: Implement fullscreen
    }

    private async void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleFavoriteAsync();
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Скачать видео",
            PrimaryButtonText = "Скачать",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.StartDownloadAsync();
        }
    }

    private void OnRelatedVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }
}
