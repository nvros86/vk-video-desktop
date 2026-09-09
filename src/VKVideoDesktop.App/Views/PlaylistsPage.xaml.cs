using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class PlaylistsPage : Page
{
    public PlaylistsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // TODO: Load playlists
    }

    private void OnPlaylistClick(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate to playlist detail
    }

    private async void OnCreateClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Новый плейлист",
            PrimaryButtonText = "Создать",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var textBox = new TextBox { PlaceholderText = "Название плейлиста", Margin = new Thickness(0, 12, 0, 0) };
        dialog.Content = textBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            // TODO: Create playlist
        }
    }
}
