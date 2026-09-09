using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class VideoViewModel : ViewModelBase
{
    private readonly VideoService _videoService;
    private readonly IFavoritesService _favoritesService;
    private readonly DownloadService _downloadService;
    private readonly ILogger<VideoViewModel> _logger;

    private Video? _currentVideo;
    private bool _isLoading;
    private bool _isFavorite;

    public VideoViewModel(
        VideoService videoService,
        IFavoritesService favoritesService,
        DownloadService downloadService,
        ILogger<VideoViewModel> logger)
    {
        _videoService = videoService;
        _favoritesService = favoritesService;
        _downloadService = downloadService;
        _logger = logger;
    }

    public VideoDisplayViewModel CurrentVideo { get; } = new();
    public ObservableCollection<VideoDisplayViewModel> RelatedVideos { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    public async Task LoadVideoAsync(string videoId)
    {
        try
        {
            IsLoading = true;

            var video = await _videoService.GetVideoAsync(videoId);
            if (video == null) return;

            _currentVideo = video;
            CurrentVideo.UpdateFrom(video);
            IsFavorite = await _favoritesService.IsFavoriteAsync(videoId);

            if (!string.IsNullOrEmpty(video.ChannelId))
            {
                var related = await _videoService.GetChannelVideosAsync(video.ChannelId);
                RelatedVideos.Clear();
                foreach (var v in related.Where(v => v.Id != videoId).Take(10))
                {
                    var vm = new VideoDisplayViewModel();
                    vm.UpdateFrom(v);
                    RelatedVideos.Add(vm);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load video {VideoId}", videoId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ToggleFavoriteAsync()
    {
        if (_currentVideo == null) return;

        if (IsFavorite)
        {
            await _favoritesService.RemoveAsync(_currentVideo.Id);
            IsFavorite = false;
        }
        else
        {
            await _favoritesService.AddAsync(new FavoriteEntry
            {
                VideoId = _currentVideo.Id,
                Title = _currentVideo.Title,
                Author = _currentVideo.ChannelName ?? string.Empty,
                ThumbnailUrl = _currentVideo.ThumbnailUrl,
                Duration = _currentVideo.Duration,
                AddedAt = DateTime.UtcNow
            });
            IsFavorite = true;
        }
    }

    public async Task StartDownloadAsync()
    {
        if (_currentVideo == null) return;

        var options = await _downloadService.GetAvailableDownloadsAsync(_currentVideo);
        var best = options.OrderByDescending(o => o.Size).FirstOrDefault();

        if (best != null)
        {
            await _downloadService.StartDownloadAsync(_currentVideo, best);
        }
    }
}

public sealed class VideoDisplayViewModel : ViewModelBase
{
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _thumbnailUrl = string.Empty;
    private string _author = string.Empty;
    private string _channelAvatarUrl = string.Empty;
    private string _durationText = string.Empty;
    private string _viewCountText = string.Empty;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string ThumbnailUrl { get => _thumbnailUrl; set => SetProperty(ref _thumbnailUrl, value); }
    public string Author { get => _author; set => SetProperty(ref _author, value); }
    public string ChannelAvatarUrl { get => _channelAvatarUrl; set => SetProperty(ref _channelAvatarUrl, value); }
    public string DurationText { get => _durationText; set => SetProperty(ref _durationText, value); }
    public string ViewCountText { get => _viewCountText; set => SetProperty(ref _viewCountText, value); }

    public void UpdateFrom(Core.Models.Video video)
    {
        Id = video.Id;
        Title = video.Title;
        Description = video.Description;
        ThumbnailUrl = video.ThumbnailUrl;
        Author = video.ChannelName ?? string.Empty;
        DurationText = FormatDuration(video.Duration);
        ViewCountText = FormatViewCount(video.ViewCount);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
    }

    private static string FormatViewCount(long count)
    {
        if (count >= 1_000_000)
            return $"{count / 1_000_000.0:F1} млн просмотров";
        if (count >= 1_000)
            return $"{count / 1_000.0:F0} тыс. просмотров";
        return $"{count} просмотров";
    }
}
