using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class VideoViewModel_Service : ViewModelBase
{
    private readonly VideoService _videoService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<VideoViewModel_Service> _logger;

    private Video? _currentVideo;
    private bool _isLoading;
    private bool _isFavorite;

    public VideoViewModel_Service(
        VideoService videoService,
        IFavoritesService favoritesService,
        ILogger<VideoViewModel_Service> logger)
    {
        _videoService = videoService;
        _favoritesService = favoritesService;
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
}
