using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class VideoPage : Page
{
    public VideoViewModel ViewModel { get; }
    private readonly PlaybackService _playbackService;
    private Windows.Media.Playback.MediaPlayer? _mediaPlayer;
    private bool _isFullscreen;

    public VideoPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<VideoViewModel>();
        _playbackService = App.GetService<PlaybackService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string videoId)
        {
            await ViewModel.LoadVideoAsync(videoId);

            if (!string.IsNullOrEmpty(ViewModel.CurrentVideo.PlaybackUrl))
            {
                StartPlayback(ViewModel.CurrentVideo.PlaybackUrl);
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        CleanupMediaPlayer();
    }

    private void StartPlayback(string url)
    {
        try
        {
            CleanupMediaPlayer();

            _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            _mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(url));
            _mediaPlayer.Play();

            _mediaPlayer.MediaOpened += OnMediaPlayerOpened;
            _mediaPlayer.MediaFailed += OnMediaPlayerFailed;
            _mediaPlayer.CurrentStateChanged += OnCurrentStateChanged;

            MediaPlayerElement.SetMediaPlayer(_mediaPlayer);
            MediaPlayerElement.Visibility = Visibility.Visible;
            ThumbnailImage.Visibility = Visibility.Collapsed;
            PlayButton.Visibility = Visibility.Collapsed;

            _playbackService.UpdateState(
                new Video
                {
                    Id = ViewModel.CurrentVideo.Id,
                    Title = ViewModel.CurrentVideo.Title,
                    Duration = ViewModel.CurrentVideo.Duration
                },
                url);

            var queueIds = new List<string> { ViewModel.CurrentVideo.Id };
            queueIds.AddRange(ViewModel.RelatedVideos.Select(v => v.Id).Where(id => id != ViewModel.CurrentVideo.Id));
            _playbackService.SetQueue(queueIds, 0);
        }
        catch (Exception)
        {
        }
    }

    private void CleanupMediaPlayer()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            _mediaPlayer.MediaOpened -= OnMediaPlayerOpened;
            _mediaPlayer.MediaFailed -= OnMediaPlayerFailed;
            _mediaPlayer.CurrentStateChanged -= OnCurrentStateChanged;
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
    }

    private void OnMediaPlayerOpened(object? sender, object e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_mediaPlayer?.PlaybackSession != null)
            {
                var duration = _mediaPlayer.PlaybackSession.NaturalDuration;
                if (duration.TotalSeconds > 0)
                {
                    UpdateTimeDisplay(TimeSpan.Zero, duration);
                }
            }
        });
    }

    private void OnMediaPlayerFailed(object? sender, object e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MediaPlayerElement.Visibility = Visibility.Collapsed;
            ThumbnailImage.Visibility = Visibility.Visible;
            PlayButton.Visibility = Visibility.Visible;
        });
    }

    private void OnCurrentStateChanged(object? sender, object e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_mediaPlayer?.PlaybackSession != null)
            {
                switch (_mediaPlayer.PlaybackSession.PlaybackState)
                {
                    case Windows.Media.Playback.MediaPlaybackState.Playing:
                        PlayIcon.Glyph = "\uE769";
                        _playbackService.State.IsPlaying = true;
                        break;
                    case Windows.Media.Playback.MediaPlaybackState.Paused:
                        PlayIcon.Glyph = "\uE768";
                        _playbackService.State.IsPlaying = false;
                        break;
                }

                var pos = _mediaPlayer.PlaybackSession.Position;
                var dur = _mediaPlayer.PlaybackSession.NaturalDuration;
                if (dur.TotalSeconds > 0)
                {
                    UpdateTimeDisplay(pos, dur);
                    PositionBar.Maximum = dur.TotalSeconds;
                    PositionBar.Value = pos.TotalSeconds;
                }
            }
        });
    }

    private void UpdateTimeDisplay(TimeSpan position, TimeSpan duration)
    {
        TimeText.Text = $"{FormatTime(position)} / {FormatTime(duration)}";
    }

    private static string FormatTime(TimeSpan ts)
    {
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer != null)
        {
            if (_mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();
        }
        else if (!string.IsNullOrEmpty(ViewModel.CurrentVideo.PlaybackUrl))
        {
            StartPlayback(ViewModel.CurrentVideo.PlaybackUrl);
        }
    }

    private void OnVolumeClick(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
            _playbackService.SetVolume(_mediaPlayer.IsMuted ? 0 : _mediaPlayer.Volume);
        }
    }

    private void OnFullscreenClick(object sender, RoutedEventArgs e)
    {
        var window = App.GetService<MainWindow>();
        _isFullscreen = !_isFullscreen;

        if (_isFullscreen)
        {
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = false;
            window.SystemBackdrop = null;
            var presenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
            presenter.IsAlwaysOnTop = false;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsResizable = true;
            window.AppWindow.SetPresenter(presenter);
        }
        else
        {
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = false;
            window.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            window.AppWindow.SetPresenter(Microsoft.UI.Windowing.OverlappedPresenter.Create());
        }
    }

    private async void OnFavoriteClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleFavoriteAsync();
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Скачать видео",
            PrimaryButtonText = "Скачать",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.StartDownloadAsync();
        }
    }

    private void OnRelatedVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }
}
