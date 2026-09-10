using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class PlaylistsPage : Page
{
    private readonly IPlaylistService _playlistService;

    public PlaylistsPage()
    {
        InitializeComponent();
        _playlistService = App.GetService<IPlaylistService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var playlists = await _playlistService.GetAllAsync();
        PlaylistsList.ItemsSource = playlists;
        EmptyState.Visibility = playlists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PlaylistsList.Visibility = playlists.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPlaylistClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string playlistId)
        {
            Frame.Navigate(typeof(VideoPage), playlistId);
        }
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
            await _playlistService.CreateAsync(textBox.Text, null);
            OnLoaded(this, new RoutedEventArgs());
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string playlistId)
        {
            var dialog = new ContentDialog
            {
                Title = "Удалить плейлист",
                Content = "Вы уверены?",
                PrimaryButtonText = "Удалить",
                CloseButtonText = "Отмена",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _playlistService.DeleteAsync(playlistId);
                OnLoaded(this, new RoutedEventArgs());
            }
        }
    }
}
