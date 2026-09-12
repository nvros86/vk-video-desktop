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
            var settings = App.GetService<ISettingsService>();
            var videoProvider = App.GetService<IVideoProvider>();

            if (!string.IsNullOrEmpty(settings.Settings.AccessToken))
            {
                var channel = await videoProvider.GetChannelAsync("0", CancellationToken.None);
                if (channel != null)
                {
                    ViewModel.UserName = channel.Name;
                    ViewModel.UserDomain = $"@{channel.Username}";
                    ViewModel.AvatarUrl = channel.AvatarUrl ?? "";

                    if (!string.IsNullOrEmpty(channel.AvatarUrl))
                    {
                        try
                        {
                            AvatarBrush.ImageSource = new BitmapImage(new Uri(channel.AvatarUrl));
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ProfilePage] Failed to load profile info");
        }
    }

    private void OnProfileNavSelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag?.ToString())
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
            }
        }
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        var settings = App.Services.GetRequiredService<Core.Interfaces.ISettingsService>();
        settings.Settings.AccessToken = "";
        _ = settings.SaveAsync();

        Frame.Navigate(typeof(LoginPage));
    }
}
