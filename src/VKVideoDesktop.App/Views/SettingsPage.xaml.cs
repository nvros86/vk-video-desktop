using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VKVideoDesktop.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnClearCacheClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить кэш",
            Content = "Вы уверены, что хотите очистить кэш изображений?",
            PrimaryButtonText = "Очистить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            // TODO: Clear cache
        }
    }
}
