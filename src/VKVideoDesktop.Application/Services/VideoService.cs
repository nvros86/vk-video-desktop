using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class VideoService
{
    private readonly IVideoProvider _videoProvider;
    private readonly IHistoryService _historyService;
    private readonly ILogger<VideoService> _logger;

    public VideoService(
        IVideoProvider videoProvider,
        IHistoryService historyService,
        ILogger<VideoService> logger)
    {
        _videoProvider = videoProvider;
        _historyService = historyService;
        _logger = logger;
    }

    public async Task<Video?> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _videoProvider.GetVideoAsync(videoId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video {VideoId}", videoId);
            return null;
        }
    }

    public async Task<Channel?> GetChannelAsync(
        string channelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _videoProvider.GetChannelAsync(channelId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channel {ChannelId}", channelId);
            return null;
        }
    }

    public async Task<IReadOnlyList<Video>> GetChannelVideosAsync(
        string channelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _videoProvider.GetVideosByChannelAsync(channelId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
    }

    public async Task RecordPlaybackAsync(
        string videoId,
        string title,
        string author,
        string thumbnailUrl,
        TimeSpan duration,
        TimeSpan position,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new HistoryEntry
            {
                VideoId = videoId,
                Title = title,
                Author = author,
                ThumbnailUrl = thumbnailUrl,
                Duration = duration,
                LastPosition = position,
                LastViewed = DateTime.UtcNow
            };

            await _historyService.SaveOrUpdateAsync(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record playback for {VideoId}", videoId);
        }
    }
}
