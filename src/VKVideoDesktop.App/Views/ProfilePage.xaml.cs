using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class ProfilePage : Page
{
    public ProfileViewModel ViewModel { get; } = new();

    public ProfilePage()
    {
        InitializeComponent();
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
