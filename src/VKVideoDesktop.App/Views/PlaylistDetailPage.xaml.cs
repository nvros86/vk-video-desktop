using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class PlaylistDetailPage : Page
{
    private readonly IPlaylistService _playlistService;
    private readonly LocalizationService _localization;
    private string _playlistId = string.Empty;

    public PlaylistDetailPage()
    {
        InitializeComponent();
        _playlistService = App.GetService<IPlaylistService>();
        _localization = App.GetService<LocalizationService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string playlistId)
        {
            _playlistId = playlistId;
            await LoadPlaylistAsync();
        }
    }

    private async Task LoadPlaylistAsync()
    {
        var playlist = await _playlistService.GetByIdAsync(_playlistId);
        if (playlist == null) return;

        PlaylistTitle.Text = playlist.Title;
        VideoCountLabel.Text = string.Format(_localization["PlaylistDetailVideoCount"], playlist.Videos.Count);

        if (playlist.Videos.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            VideosList.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyState.Visibility = Visibility.Collapsed;
        VideosList.Visibility = Visibility.Visible;

        var videos = new List<VideoViewModel>();
        foreach (var vid in playlist.Videos)
        {
            videos.Add(new VideoViewModel
            {
                Id = vid.Id,
                Title = vid.Title ?? "",
                Author = vid.ChannelName ?? "",
                ThumbnailUrl = vid.ThumbnailUrl ?? "",
                DurationText = vid.Duration.TotalHours >= 1
                    ? $"{(int)vid.Duration.TotalHours}:{vid.Duration.Minutes:D2}:{vid.Duration.Seconds:D2}"
                    : $"{(int)vid.Duration.TotalMinutes}:{vid.Duration.Seconds:D2}"
            });
        }

        VideosList.ItemsSource = videos;
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void OnVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            await _playlistService.RemoveVideoAsync(_playlistId, videoId);
            await LoadPlaylistAsync();
        }
    }
}
