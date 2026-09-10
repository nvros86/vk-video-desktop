using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.App.Views;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using Microsoft.UI.Windowing;
using Windows.System;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    private bool _isDownloadsPanelOpen;
    private MiniPlayerWindow? _miniPlayerWindow;
    private IAuthenticationService? _authService;
    private PlaybackService? _playbackService;

    internal void InitServices()
    {
        _authService = App.GetService<IAuthenticationService>();
        _playbackService = App.GetService<PlaybackService>();
        _playbackService.StateChanged += OnPlaybackStateChanged;
    }

    private readonly Dictionary<string, Type> _pageMap = new()
    {
        ["Home"] = typeof(HomePage),
        ["Search"] = typeof(SearchPage),
        ["Profile"] = typeof(ProfilePage),
        ["Favorites"] = typeof(FavoritesPage),
        ["Favorites2"] = typeof(FavoritesPage),
        ["Playlists"] = typeof(PlaylistsPage),
        ["History"] = typeof(HistoryPage),
        ["Downloads"] = typeof(DownloadsPage),
        ["Settings"] = typeof(SettingsPage)
    };

    private static void MwLog(string msg)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VKVideoDesktop", "log");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "debug.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainWindow] {msg}\n");
        }
        catch { }
    }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        Title = "VK Video Desktop";
        ExtendsContentIntoTitleBar = false;
        ContentFrame.Navigated += OnFrameNavigated;
        ContentFrame.KeyDown += OnGlobalKeyDown;
    }

    private void OnGlobalKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        bool isCtrlPressed = ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        bool isShiftPressed = shift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (isCtrlPressed && e.Key == VirtualKey.K)
        {
            SearchBox?.Focus(FocusState.Programmatic);
            e.Handled = true;
            return;
        }

        if (ContentFrame.Content is VideoPage videoPage)
        {
            switch (e.Key)
            {
                case VirtualKey.Space:
                case VirtualKey.K:
                    videoPage.TogglePlayPause();
                    e.Handled = true;
                    break;

                case VirtualKey.F:
                    videoPage.ToggleFullscreen();
                    e.Handled = true;
                    break;

                case VirtualKey.M:
                    videoPage.ToggleMute();
                    e.Handled = true;
                    break;

                case VirtualKey.Left:
                    videoPage.SeekRelative(isShiftPressed ? -10000 : -5000);
                    e.Handled = true;
                    break;

                case VirtualKey.Right:
                    videoPage.SeekRelative(isShiftPressed ? 10000 : 5000);
                    e.Handled = true;
                    break;

                case VirtualKey.Up:
                    videoPage.AdjustVolume(5);
                    e.Handled = true;
                    break;

                case VirtualKey.Down:
                    videoPage.AdjustVolume(-5);
                    e.Handled = true;
                    break;

                case VirtualKey.Escape:
                    if (videoPage.IsFullscreen)
                    {
                        videoPage.ToggleFullscreen();
                        e.Handled = true;
                    }
                    break;
            }
        }
        else
        {
            if (e.Key == VirtualKey.Escape)
            {
                SearchBox.Text = "";
                e.Handled = true;
            }
        }
    }

    private void NavigateToVideoPage()
    {
        if (_playbackService?.State.CurrentVideoId != null)
        {
            ContentFrame.Navigate(typeof(VideoPage), _playbackService.State.CurrentVideoId);
        }
    }

    private void OnPlaybackStateChanged(object? sender, Core.Models.PlaybackState state)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MiniPlayerBar.Visibility = state.CurrentVideoId != null ? Visibility.Visible : Visibility.Collapsed;
            MiniPlayerTitle.Text = state.CurrentVideoId ?? string.Empty;
            _miniPlayerWindow?.UpdateTitle(state.CurrentVideoId ?? "VK Video");
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
        else if (e.SourcePageType == typeof(ProfilePage))
            NavView.SelectedItem = NavView.MenuItems[2];
        else if (e.SourcePageType == typeof(FavoritesPage))
            NavView.SelectedItem = NavView.MenuItems[3];
        else if (e.SourcePageType == typeof(PlaylistsPage))
            NavView.SelectedItem = NavView.MenuItems[5];
        else if (e.SourcePageType == typeof(HistoryPage))
            NavView.SelectedItem = NavView.MenuItems[6];
        else if (e.SourcePageType == typeof(DownloadsPage))
            NavView.SelectedItem = NavView.MenuItems[7];
        else if (e.SourcePageType == typeof(SettingsPage))
            NavView.SelectedItem = NavView.MenuItems[8];
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
        var prevId = _playbackService.GetPreviousVideoId();
        if (prevId != null)
        {
            ContentFrame.Navigate(typeof(VideoPage), prevId);
        }
    }

    private void OnMiniPlayerNextClick(object sender, RoutedEventArgs e)
    {
        var nextId = _playbackService.GetNextVideoId();
        if (nextId != null)
        {
            ContentFrame.Navigate(typeof(VideoPage), nextId);
        }
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
        if (_miniPlayerWindow == null)
        {
            _miniPlayerWindow = new MiniPlayerWindow();
            _miniPlayerWindow.Closed += (_, _) => _miniPlayerWindow = null;
            _miniPlayerWindow.UpdateTitle(_playbackService.State.CurrentVideoId ?? "VK Video");
            _miniPlayerWindow.Activate();
        }
        else
        {
            _miniPlayerWindow.Activate();
        }
    }

    public void GoToFullscreen()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var presenter = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId).Presenter
            as Microsoft.UI.Windowing.OverlappedPresenter;

        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.Maximize();
        }
    }

    public void RestoreFromFullscreen()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var presenter = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId).Presenter
            as Microsoft.UI.Windowing.OverlappedPresenter;

        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(true, true);
            presenter.Restore();
        }
    }

    public void ShowError(string message, int autoHideMs = 5000)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            ErrorBannerText.Text = message;
            ErrorBanner.Visibility = Visibility.Visible;

            if (autoHideMs > 0)
            {
                await Task.Delay(autoHideMs);
                ErrorBanner.Visibility = Visibility.Collapsed;
            }
        });
    }

    private void OnCloseErrorBanner(object sender, RoutedEventArgs e)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
    }

    public void NavigateToHome()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ContentFrame.Navigate(typeof(HomePage));
        });
    }

    internal async Task HandleDeepLinkAsync(DeepLinkResult result)
    {
        switch (result.Type)
        {
            case DeepLinkType.Video:
                if (string.IsNullOrEmpty(result.VideoId)) break;
                var videoService = App.Services.GetRequiredService<VideoService>();
                var video = await videoService.GetVideoAsync(result.VideoId);
                if (video != null)
                    ContentFrame.Navigate(typeof(VideoPage), video);
                break;

            case DeepLinkType.Search:
                if (string.IsNullOrEmpty(result.Query)) break;
                ContentFrame.Navigate(typeof(SearchPage), result.Query);
                break;

            case DeepLinkType.Channel:
                if (string.IsNullOrEmpty(result.ChannelId)) break;
                ContentFrame.Navigate(typeof(ChannelPage), result.ChannelId);
                break;
        }
    }
}
