using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class FavoritesPage : Page
{
    private readonly IFavoritesService _favoritesService;

    public FavoritesPage()
    {
        InitializeComponent();
        _favoritesService = App.GetService<IFavoritesService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var favorites = await _favoritesService.GetAllAsync();
        FavoritesList.ItemsSource = favorites;
        EmptyState.Visibility = favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        FavoritesList.Visibility = favorites.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            await _favoritesService.RemoveAsync(videoId);
            OnLoaded(this, new RoutedEventArgs());
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
            var favorites = await _favoritesService.GetAllAsync();
            foreach (var fav in favorites)
            {
                await _favoritesService.RemoveAsync(fav.VideoId);
            }
            OnLoaded(this, new RoutedEventArgs());
        }
    }
}
