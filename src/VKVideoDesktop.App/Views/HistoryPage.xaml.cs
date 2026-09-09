using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TODO: Load history
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        // TODO: Remove from history
    }

    private async void OnClearClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить историю",
            Content = "Вы уверены, что хотите очистить всю историю просмотров?",
            PrimaryButtonText = "Очистить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // TODO: Clear history
        }
    }
}
