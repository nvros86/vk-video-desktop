using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class FavoritesPage : Page
{
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<FavoritesPage> _logger;
    private readonly LocalizationService _localization;

    public FavoritesPage()
    {
        _logger = App.GetService<ILogger<FavoritesPage>>();
        _logger.LogInformation("[FavoritesPage] Constructor");
        InitializeComponent();
        _favoritesService = App.GetService<IFavoritesService>();
        _localization = App.GetService<LocalizationService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var favorites = await _favoritesService.GetAllAsync();
        FavoritesItemsControl.ItemsSource = favorites;
        EmptyState.Visibility = favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnVideoItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }
}
