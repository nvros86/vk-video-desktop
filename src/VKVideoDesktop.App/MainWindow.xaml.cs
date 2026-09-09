using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.App.Views;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow : Window
{
    private bool _isDownloadsPanelOpen;
    public string MiniPlayerThumbnail { get; set; } = string.Empty;

    private readonly Dictionary<string, Type> _pageMap = new()
    {
        ["Home"] = typeof(HomePage),
        ["Search"] = typeof(SearchPage),
        ["MyVideos"] = typeof(HomePage),
        ["Subscriptions"] = typeof(HomePage),
        ["Favorites"] = typeof(FavoritesPage),
        ["Playlists"] = typeof(PlaylistsPage),
        ["History"] = typeof(HistoryPage),
        ["Downloads"] = typeof(DownloadsPage)
    };

    public MainWindow()
    {
        InitializeComponent();
        Title = "VK Video Desktop";

        ContentFrame.Navigated += OnFrameNavigated;
        ContentFrame.Navigate(typeof(HomePage));

        // Global keyboard shortcuts
        var accelerator1 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.K, Modifiers = Windows.System.VirtualKeyModifiers.Control };
        var accelerator2 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.J, Modifiers = Windows.System.VirtualKeyModifiers.Control };
        var accelerator3 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.F };
        var accelerator4 = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };

        // Set title bar
        ExtendsContentIntoTitleBar = false;
    }

    private void OnNavViewLoaded(object sender, RoutedEventArgs e)
    {
        // Select Home by default
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            if (_pageMap.TryGetValue(tag, out var pageType))
            {
                if (ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        // Update nav view selection based on current page
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && SearchBox.Text is { Length: > 0 } query)
        {
            ContentFrame.Navigate(typeof(SearchPage), query);
            NavView.SelectedItem = NavView.MenuItems[1]; // Search
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            SearchBox.Text = string.Empty;
        }
    }

    private void OnDownloadsClick(object sender, RoutedEventArgs e)
    {
        _isDownloadsPanelOpen = !_isDownloadsPanelOpen;
        DownloadsPanel.Visibility = _isDownloadsPanelOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCloseDownloadsPanel(object sender, RoutedEventArgs e)
    {
        _isDownloadsPanelOpen = false;
        DownloadsPanel.Visibility = Visibility.Collapsed;
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(SettingsPage));
    }

    private void OnMiniPlayerPlayClick(object sender, RoutedEventArgs e)
    {
        // TODO: Toggle play/pause
    }

    private void OnMiniPlayerPrevClick(object sender, RoutedEventArgs e)
    {
        // TODO: Previous video
    }

    private void OnMiniPlayerNextClick(object sender, RoutedEventArgs e)
    {
        // TODO: Next video
    }

    private void OnMiniPlayerFullscreenClick(object sender, RoutedEventArgs e)
    {
        // TODO: Fullscreen
    }

    private void OnMiniPlayerCloseClick(object sender, RoutedEventArgs e)
    {
        MiniPlayerBar.Visibility = Visibility.Collapsed;
    }
}
