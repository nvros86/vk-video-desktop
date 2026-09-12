using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class HomePage : Page
{
    public MainViewModel ViewModel { get; }
    private readonly ILogger<HomePage> _logger;
    private readonly LocalizationService _localization;
    private bool _isGuestMode;
    private string _currentVkSection = "";

    private const string VkHideSidebarCss = @"
        (function() {
            var style = document.createElement('style');
            style.textContent = `
                nav-left-menu, .leftMenu, .LeftMenu, .vk-left-menu,
                [class*=""LeftBlock""], [class*=""leftBlock""],
                [class*=""left_menu""], [class*=""left-menu""],
                [class*=""VKNavigation""], [class*=""vkNavigation""],
                .mosaic-player, .VideoFilters, .VideoFilters__title,
                header.Header, .Header, [class*=""TopNav""],
                [class*=""topNav""], [class*=""app_header""],
                .VideoPage__left, .videoplayer_ui__nav {
                    display: none !important;
                }
                .VideoPage, .VideoPage__content,
                .VideosSection, .VideosSection__content,
                .video_grid, .VideoCards,
                .VideoCard, .VideoCard__info,
                .VideoCardList, .VideoCardList__card,
                .video_list, .video_row {
                    width: 100% !important;
                    max-width: 100% !important;
                }
                body {
                    overflow-x: hidden !important;
                }
            `;
            document.head.appendChild(style);
        })();";

    public HomePage()
    {
        _logger = App.GetService<ILogger<HomePage>>();
        _logger.LogInformation("[HomePage] Constructor");
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();
        _localization = App.GetService<LocalizationService>();
        Loaded += OnLoaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _logger.LogInformation("[HomePage] OnNavigatedTo");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = App.GetService<ISettingsService>();
        _isGuestMode = string.IsNullOrEmpty(settings.Settings.AccessToken);

        if (_isGuestMode)
        {
            _logger.LogInformation("[HomePage] Guest mode - showing WebView2 with vk.com/video");
            AuthorizedFeed.Visibility = Visibility.Collapsed;
            GuestWebView.Visibility = Visibility.Visible;
            await InitGuestWebViewAsync();
        }
        else
        {
            _logger.LogInformation("[HomePage] Authorized - loading native feed");
            AuthorizedFeed.Visibility = Visibility.Visible;
            GuestWebView.Visibility = Visibility.Collapsed;
            await LoadAuthorizedFeedAsync();
        }
    }

    private async System.Threading.Tasks.Task InitGuestWebViewAsync()
    {
        try
        {
            var env = await WebView2Helper.GetEnvironmentAsync();
            await GuestWebView2.EnsureCoreWebView2Async(env);

            GuestWebView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            GuestWebView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            GuestWebView2.CoreWebView2.Settings.IsZoomControlEnabled = true;

            GuestWebView2.CoreWebView2.NewWindowRequested += OnWebViewNewWindowRequested;
            GuestWebView2.CoreWebView2.NavigationCompleted += OnGuestWebViewNavigationCompleted;

            NavigateToVkSection("home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] Failed to init Guest WebView2");
        }
    }

    private void OnGuestWebViewNavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        _ = InjectCssToHideVkSidebar();
    }

    private async System.Threading.Tasks.Task InjectCssToHideVkSidebar()
    {
        try
        {
            if (GuestWebView2.CoreWebView2 != null)
            {
                await GuestWebView2.CoreWebView2.ExecuteScriptAsync(VkHideSidebarCss);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[HomePage] CSS injection failed (non-critical)");
        }
    }

    public void NavigateToVkSection(string section)
    {
        if (!_isGuestMode || GuestWebView2.CoreWebView2 == null) return;

        _currentVkSection = section;
        string url = section switch
        {
            "home" => "https://vk.com/video",
            "trending" => "https://vk.com/video?section=trending",
            "clips" => "https://vk.com/clips",
            "music" => "https://vk.com/video?section= music_video",
            "movies" => "https://vk.com/video?section=movies",
            _ => "https://vk.com/video"
        };

        GuestWebView2.CoreWebView2.Navigate(url);
    }

    private async System.Threading.Tasks.Task LoadAuthorizedFeedAsync()
    {
        _logger.LogInformation("[HomePage] OnLoaded - loading recommendations...");
        try
        {
            await ViewModel.LoadRecommendationsAsync();
            _logger.LogInformation("[HomePage] Loaded: History={HistoryCount}, Recommendations={RecommendationsCount}",
                ViewModel.HistoryEntries.Count, ViewModel.Recommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HomePage] OnLoaded failed");
        }
    }

    private void OnWebViewNewWindowRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (!string.IsNullOrEmpty(e.Uri))
        {
            GuestWebView2.CoreWebView2.Navigate(e.Uri);
        }
    }

    private void OnVideoItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private void OnHistoryItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private void OnContextPlay(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnContextFavorite(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            var favorites = App.GetService<IFavoritesService>();
            var existing = await favorites.GetAllAsync();
            if (existing.Any(f => f.VideoId == videoId))
            {
                await favorites.RemoveAsync(videoId);
            }
            else
            {
                await favorites.AddAsync(new FavoriteEntry
                {
                    VideoId = videoId,
                    Title = "Video",
                    Author = "",
                    AddedAt = DateTime.UtcNow
                });
            }
        }
    }

    private void OnContextDownload(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnContextOpenInVk(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://vk.com/video{videoId}"));
        }
    }

    private void OnContextHistoryPlay(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnContextHistoryFavorite(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            var favorites = App.GetService<IFavoritesService>();
            var existing = await favorites.GetAllAsync();
            if (existing.Any(f => f.VideoId == videoId))
            {
                await favorites.RemoveAsync(videoId);
            }
            else
            {
                await favorites.AddAsync(new FavoriteEntry
                {
                    VideoId = videoId,
                    Title = "Video",
                    Author = "",
                    AddedAt = DateTime.UtcNow
                });
            }
        }
    }

    private async void OnContextHistoryRemove(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            var history = App.GetService<IHistoryService>();
            var entry = await history.GetByVideoIdAsync(videoId);
            if (entry != null)
            {
                await history.DeleteAsync(entry.Id);
                await ViewModel.LoadRecommendationsAsync();
            }
        }
    }

    private async void OnContextHistoryOpenInVk(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://vk.com/video{videoId}"));
        }
    }

    private void OnAddToQueueClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is VideoViewModel video)
        {
            var playbackService = App.Services.GetRequiredService<PlaybackService>();
            playbackService.Enqueue(video.Id);
        }
    }

    private void OnLoginButtonClick(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("[HomePage] Login button clicked");
        Frame.Navigate(typeof(LoginPage));
    }

    private async void OnAddToPlaylistClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is VideoViewModel video)
        {
            var playlistService = App.Services.GetRequiredService<IPlaylistService>();
            var playlists = await playlistService.GetAllAsync();
            if (playlists.Count > 0)
            {
                var dialog = new ContentDialog();
                dialog.Title = _localization["HomeAddToPlaylist"];
                var listView = new ListView();
                foreach (var pl in playlists)
                    listView.Items.Add(new ListViewItem { Content = pl.Title, Tag = pl });
                listView.SelectionChanged += async (s, args) =>
                {
                    if (listView.SelectedItem is ListViewItem selected && selected.Tag is Playlist playlist)
                    {
                        var videoObj = new Video
                        {
                            Id = video.Id,
                            Title = video.Title,
                            ChannelName = video.Author,
                            ThumbnailUrl = video.ThumbnailUrl,
                            Duration = TimeSpan.Zero
                        };
                        await playlistService.AddVideoAsync(playlist.Id, videoObj);
                        dialog.Hide();
                    }
                };
                dialog.Content = listView;
                dialog.PrimaryButtonText = _localization["Cancel"];
                await dialog.ShowAsync();
            }
        }
    }

    private void OnShareClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is VideoViewModel video)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText($"https://vk.com/video{video.Id}");
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }
}
