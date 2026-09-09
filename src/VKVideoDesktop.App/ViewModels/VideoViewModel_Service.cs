using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class VideoViewModel_Service : ViewModelBase
{
    private readonly VideoService _videoService;
    private readonly IFavoritesService _favoritesService;
    private readonly IDownloadService _downloadService;
    private readonly ILogger<VideoViewModel_Service> _logger;

    private Video? _currentVideo;
    private bool _isLoading;
    private bool _isFavorite;

    public VideoViewModel_Service(
        VideoService videoService,
        IFavoritesService favoritesService,
        IDownloadService downloadService,
        ILogger<VideoViewModel_Service> logger)
    {
        _videoService = videoService;
        _favoritesService = favoritesService;
        _downloadService = downloadService;
        _logger = logger;
    }

    public VideoViewModel CurrentVideo { get; } = new();
    public ObservableCollection<VideoViewModel> RelatedVideos { get; } = new();

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

            // Load related videos
            if (!string.IsNullOrEmpty(video.ChannelId))
            {
                var related = await _videoService.GetChannelVideosAsync(video.ChannelId);
                RelatedVideos.Clear();
                foreach (var v in related.Where(v => v.Id != videoId).Take(10))
                {
                    var vm = new VideoViewModel();
                    vm.UpdateFromRelated(v);
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

public interface IDownloadService
{
    Task<IReadOnlyList<DownloadOption>> GetAvailableDownloadsAsync(Video video, CancellationToken cancellationToken = default);
    Task<DownloadTask?> StartDownloadAsync(Video video, DownloadOption option, CancellationToken cancellationToken = default);
}
