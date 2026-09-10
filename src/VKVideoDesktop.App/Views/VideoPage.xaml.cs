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
    private bool _isPip;
    private Dictionary<string, string> _availableQualities = new();
    private string _currentQualityKey = "720";
    private double _currentSpeed = 1.0;
    private static readonly double[] SpeedOptions = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0 };
    private int _speedIndex = 3;

    public VideoPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<VideoViewModel>();
        _playbackService = App.GetService<PlaybackService>();
        _playbackService.PlaybackCompleted += OnPlaybackCompleted;
    }

    public bool IsFullscreen { get; private set; } = false;

    public void TogglePlayPause()
    {
        if (MediaPlayerElement == null) return;

        var player = MediaPlayerElement.MediaPlayer;
        if (player == null) return;

        if (player.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
            player.Pause();
        else
            player.Play();
    }

    public void ToggleFullscreen()
    {
        if (MainWindow.Instance == null) return;

        if (IsFullscreen)
        {
            MainWindow.Instance.RestoreFromFullscreen();
            IsFullscreen = false;
        }
        else
        {
            MainWindow.Instance.GoToFullscreen();
            IsFullscreen = true;
        }
    }

    public void ToggleMute()
    {
        if (MediaPlayerElement?.MediaPlayer == null) return;
        MediaPlayerElement.MediaPlayer.IsMuted = !MediaPlayerElement.MediaPlayer.IsMuted;
    }

    public void SeekRelative(int milliseconds)
    {
        if (MediaPlayerElement?.MediaPlayer?.PlaybackSession == null) return;
        var session = MediaPlayerElement.MediaPlayer.PlaybackSession;
        var newPos = session.Position + TimeSpan.FromMilliseconds(milliseconds);
        if (newPos < TimeSpan.Zero) newPos = TimeSpan.Zero;
        if (newPos > session.NaturalDuration) newPos = session.NaturalDuration;
        session.Position = newPos;
    }

    public void AdjustVolume(int delta)
    {
        if (MediaPlayerElement?.MediaPlayer == null) return;
        var current = MediaPlayerElement.MediaPlayer.Volume;
        var newVol = Math.Clamp(current + delta / 100.0, 0.0, 1.0);
        MediaPlayerElement.MediaPlayer.Volume = newVol;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string videoId)
        {
            await ViewModel.LoadVideoAsync(videoId);

            if (ViewModel.CurrentVideo.QualityUrls != null)
            {
                _availableQualities = ViewModel.CurrentVideo.QualityUrls;
                UpdateQualityButton();
            }

            if (!string.IsNullOrEmpty(ViewModel.CurrentVideo.PlaybackUrl))
            {
                StartPlayback(ViewModel.CurrentVideo.PlaybackUrl);
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _playbackService.PlaybackCompleted -= OnPlaybackCompleted;
        CleanupMediaPlayer();
    }

    private void StartPlayback(string url)
    {
        try
        {
            CleanupMediaPlayer();

            _mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            _mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(url));
            _mediaPlayer.PlaybackRate = _currentSpeed;
            _mediaPlayer.Play();

            _mediaPlayer.MediaOpened += OnMediaPlayerOpened;
            _mediaPlayer.MediaFailed += OnMediaPlayerFailed;
            _mediaPlayer.CurrentStateChanged += OnCurrentStateChanged;
            _mediaPlayer.MediaEnded += OnMediaEnded;

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
            _mediaPlayer.MediaEnded -= OnMediaEnded;
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
            NavigateToWebViewFallback();
        });
    }

    private void OnWebViewFallbackClick(object sender, RoutedEventArgs e)
    {
        NavigateToWebViewFallback();
    }

    private void NavigateToWebViewFallback()
    {
        if (ViewModel.CurrentVideo != null && !string.IsNullOrEmpty(ViewModel.CurrentVideo.PlaybackUrl))
        {
            Frame.Navigate(typeof(WebViewVideoPage), new Video
            {
                Id = ViewModel.CurrentVideo.Id,
                Title = ViewModel.CurrentVideo.Title,
                PlaybackUrl = ViewModel.CurrentVideo.PlaybackUrl
            });
        }
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

    private void OnMediaEnded(object? sender, object e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _playbackService.NotifyPlaybackCompleted();
        });
    }

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var nextId = _playbackService.GetNextVideoId();
            if (nextId != null)
            {
                Frame.Navigate(typeof(VideoPage), nextId);
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

    private void OnSpeedClick(object sender, RoutedEventArgs e)
    {
        _speedIndex = (_speedIndex + 1) % SpeedOptions.Length;
        _currentSpeed = SpeedOptions[_speedIndex];
        SpeedButton.Content = _currentSpeed == 1.0 ? "1x" : $"{_currentSpeed}x";

        if (_mediaPlayer != null)
        {
            _mediaPlayer.PlaybackRate = _currentSpeed;
        }
        _playbackService.SetPlaybackSpeed(_currentSpeed);
    }

    private async void OnQualityClick(object sender, RoutedEventArgs e)
    {
        if (_availableQualities.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "Качество видео",
            CloseButtonText = "Закрыть",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var panel = new StackPanel { Spacing = 8 };
        foreach (var quality in _availableQualities.Keys.OrderByDescending(k => int.TryParse(k, out var h) ? h : 0))
        {
            var btn = new Button
            {
                Content = $"{quality}p",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 8, 12, 8),
                Tag = quality
            };
            btn.Click += OnQualitySelected;
            panel.Children.Add(btn);
        }
        dialog.Content = panel;
        await dialog.ShowAsync();
    }

    private void OnQualitySelected(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string qualityKey)
        {
            _currentQualityKey = qualityKey;
            if (_availableQualities.TryGetValue(qualityKey, out var url))
            {
                var currentPosition = _mediaPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero;
                var wasPlaying = _mediaPlayer?.PlaybackSession?.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing;
                StartPlayback(url);
                if (_mediaPlayer?.PlaybackSession != null)
                {
                    _mediaPlayer.PlaybackSession.Position = currentPosition;
                    if (!wasPlaying) _mediaPlayer.Pause();
                }
            }
            UpdateQualityButton();
        }
    }

    private void UpdateQualityButton()
    {
        QualityButton.Content = _availableQualities.ContainsKey(_currentQualityKey) ? $"{_currentQualityKey}p" : "HQ";
    }

    private void OnPipClick(object sender, RoutedEventArgs e)
    {
        TogglePip();
    }

    private void TogglePip()
    {
        var window = App.GetService<MainWindow>();
        _isPip = !_isPip;

        if (_isPip)
        {
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = false;
            window.SystemBackdrop = null;
            var presenter = Microsoft.UI.Windowing.OverlappedPresenter.Create();
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
            presenter.IsResizable = true;
            presenter.SetBorderAndTitleBar(true, true);
            window.AppWindow.SetPresenter(presenter);
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 480, Height = 320 });
        }
        else
        {
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = false;
            window.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            window.AppWindow.SetPresenter(Microsoft.UI.Windowing.OverlappedPresenter.Create());
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1400, Height = 900 });
        }
    }

    private void OnFullscreenClick(object sender, RoutedEventArgs e)
    {
        ToggleFullscreen();
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
