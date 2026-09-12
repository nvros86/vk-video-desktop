using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class ProfilePage : Page
{
    public ProfileViewModel ViewModel { get; } = new();
    private readonly ILogger<ProfilePage> _logger;

    public ProfilePage()
    {
        _logger = App.GetService<ILogger<ProfilePage>>();
        _logger.LogInformation("[ProfilePage] Constructor");
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingState.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;

            var settings = App.GetService<ISettingsService>();
            var videoProvider = App.GetService<IVideoProvider>();
            var favoritesService = App.GetService<IFavoritesService>();

            if (!string.IsNullOrEmpty(settings.Settings.AccessToken))
            {
                // Authenticated user
                QuickActions.Visibility = Visibility.Visible;
                StatsRow.Visibility = Visibility.Visible;
                GuestPrompt.Visibility = Visibility.Collapsed;

                var channel = await videoProvider.GetChannelAsync("0", CancellationToken.None);
                if (channel != null)
                {
                    UserNameText.Text = channel.Name;
                    UserDomainText.Text = $"@{channel.Username}";
                    if (!string.IsNullOrEmpty(channel.Description))
                        UserStatusText.Text = channel.Description;
                    SubscribersCount.Text = FormatCount(channel.SubscriberCount);

                    if (!string.IsNullOrEmpty(channel.AvatarUrl))
                    {
                        try
                        {
                            AvatarBrush.ImageSource = new BitmapImage(new Uri(channel.AvatarUrl));
                            AvatarFallback.Visibility = Visibility.Collapsed;
                        }
                        catch
                        {
                            AvatarFallback.Visibility = Visibility.Visible;
                        }
                    }
                }

                var favorites = await favoritesService.GetAllAsync();
                FavoritesCount.Text = favorites.Count.ToString();
            }
            else
            {
                // Guest mode
                QuickActions.Visibility = Visibility.Collapsed;
                StatsRow.Visibility = Visibility.Collapsed;
                GuestPrompt.Visibility = Visibility.Visible;

                UserNameText.Text = "Гость";
                UserDomainText.Text = "";
                UserStatusText.Text = "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfilePage] Failed to load profile info");
            UserNameText.Text = "Гость";
            UserDomainText.Text = "";
            GuestPrompt.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingState.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }
    }

    private void OnGuestLoginClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(LoginPage));
    }

    private void OnQuickActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            NavigateByTag(tag);
        }
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tag)
        {
            NavigateByTag(tag);
        }
    }

    private void NavigateByTag(string tag)
    {
        switch (tag)
        {
            case "Favorites":
                Frame.Navigate(typeof(FavoritesPage));
                break;
            case "Playlists":
                Frame.Navigate(typeof(PlaylistsPage));
                break;
            case "History":
                Frame.Navigate(typeof(HistoryPage));
                break;
            case "Settings":
                Frame.Navigate(typeof(SettingsPage));
                break;
            case "MyVideos":
                Frame.Navigate(typeof(SearchPage), "");
                break;
        }
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        var settings = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
        settings.Settings.AccessToken = "";
        settings.Settings.IsAuthorized = false;
        _ = settings.SaveAsync();

        Frame.Navigate(typeof(LoginPage));
    }

    private static string FormatCount(long count)
    {
        if (count >= 1_000_000)
            return $"{count / 1_000_000.0:F1}M";
        if (count >= 1_000)
            return $"{count / 1_000.0:F1}K";
        return count.ToString();
    }
}
