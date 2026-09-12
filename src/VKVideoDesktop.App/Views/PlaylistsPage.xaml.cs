using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class PlaylistsPage : Page
{
    private readonly IPlaylistService _playlistService;
    private readonly ILogger<PlaylistsPage> _logger;
    private readonly LocalizationService _localization;

    public PlaylistsPage()
    {
        _logger = App.GetService<ILogger<PlaylistsPage>>();
        _logger.LogInformation("[PlaylistsPage] Constructor");
        InitializeComponent();
        _playlistService = App.GetService<IPlaylistService>();
        _localization = App.GetService<LocalizationService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var playlists = await _playlistService.GetAllAsync();
        PlaylistsItemsControl.ItemsSource = playlists;
        EmptyState.Visibility = playlists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPlaylistItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string playlistId)
        {
            Frame.Navigate(typeof(PlaylistDetailPage), playlistId);
        }
    }

    private async void OnCreatePlaylistClick(object sender, RoutedEventArgs e)
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
}
