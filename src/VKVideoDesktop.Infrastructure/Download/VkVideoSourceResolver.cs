using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class VkVideoSourceResolver : IDownloadSourceResolver
{
    private readonly IVideoDownloadProvider? _downloadProvider;
    private readonly ILogger<VkVideoSourceResolver> _logger;

    public VkVideoSourceResolver(
        IVideoDownloadProvider? downloadProvider,
        ILogger<VkVideoSourceResolver> logger)
    {
        _downloadProvider = downloadProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DownloadOption>> ResolveAsync(
        Video video,
        CancellationToken cancellationToken)
    {
        if (_downloadProvider == null)
        {
            _logger.LogWarning("No download provider available for video {VideoId}", video.Id);
            return Array.Empty<DownloadOption>();
        }

        try
        {
            return await _downloadProvider.GetAvailableDownloadsAsync(video, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve download sources for {VideoId}", video.Id);
            return Array.Empty<DownloadOption>();
        }
    }
}
