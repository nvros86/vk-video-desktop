using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.App.Views;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow : Window
{
    private bool _isDownloadsPanelOpen;
    private readonly IAuthenticationService _authService;
    private readonly PlaybackService _playbackService;

    private readonly Dictionary<string, Type> _pageMap = new()
    {
        ["Home"] = typeof(HomePage),
        ["Search"] = typeof(SearchPage),
        ["Favorites"] = typeof(FavoritesPage),
        ["Playlists"] = typeof(PlaylistsPage),
        ["History"] = typeof(HistoryPage),
        ["Downloads"] = typeof(DownloadsPage)
    };

    public MainWindow()
    {
        InitializeComponent();
        Title = "VK Video Desktop";
        _authService = App.GetService<IAuthenticationService>();
        _playbackService = App.GetService<PlaybackService>();

        _playbackService.StateChanged += OnPlaybackStateChanged;

        ContentFrame.Navigated += OnFrameNavigated;

        if (_authService.IsAuthenticated)
        {
            ContentFrame.Navigate(typeof(HomePage));
        }
        else
        {
            ContentFrame.Navigate(typeof(LoginPage));
        }

        ExtendsContentIntoTitleBar = false;
    }

    private void OnPlaybackStateChanged(object? sender, Core.Models.PlaybackState state)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MiniPlayerBar.Visibility = state.CurrentVideoId != null ? Visibility.Visible : Visibility.Collapsed;
            MiniPlayerTitle.Text = state.CurrentVideoId ?? string.Empty;
        });
    }

    private void OnNavViewLoaded(object sender, RoutedEventArgs e)
    {
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
        if (e.SourcePageType == typeof(HomePage))
            NavView.SelectedItem = NavView.MenuItems[0];
        else if (e.SourcePageType == typeof(SearchPage))
            NavView.SelectedItem = NavView.MenuItems[1];
        else if (e.SourcePageType == typeof(FavoritesPage))
            NavView.SelectedItem = NavView.MenuItems[2];
        else if (e.SourcePageType == typeof(PlaylistsPage))
            NavView.SelectedItem = NavView.MenuItems[3];
        else if (e.SourcePageType == typeof(HistoryPage))
            NavView.SelectedItem = NavView.MenuItems[4];
        else if (e.SourcePageType == typeof(DownloadsPage))
            NavView.SelectedItem = NavView.MenuItems[5];
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && SearchBox.Text is { Length: > 0 } query)
        {
            ContentFrame.Navigate(typeof(SearchPage), query);
            NavView.SelectedItem = NavView.MenuItems[1];
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
        _playbackService.TogglePlayPause();
    }

    private void OnMiniPlayerPrevClick(object sender, RoutedEventArgs e)
    {
    }

    private void OnMiniPlayerNextClick(object sender, RoutedEventArgs e)
    {
    }

    private void OnMiniPlayerFullscreenClick(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.CurrentSourcePageType == typeof(VideoPage))
        {
            ContentFrame.GoBack();
        }
        else
        {
            ContentFrame.Navigate(typeof(VideoPage), _playbackService.State.CurrentVideoId);
        }
    }

    private void OnMiniPlayerCloseClick(object sender, RoutedEventArgs e)
    {
        MiniPlayerBar.Visibility = Visibility.Collapsed;
    }
}
