using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class FavoritesPage : Page
{
    public FavoritesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TODO: Load favorites
    }

    private void OnVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить избранное",
            Content = "Вы уверены, что хотите очистить весь список избранного?",
            PrimaryButtonText = "Очистить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // TODO: Clear favorites
        }
    }
}
