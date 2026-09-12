using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class VkVideoDownloadProvider : IVideoDownloadProvider
{
    private readonly IDownloadEngine _engine;

    public VkVideoDownloadProvider(IDownloadEngine engine)
    {
        _engine = engine;
    }

    public Task<IReadOnlyList<DownloadOption>> GetAvailableDownloadsAsync(
        Video video,
        CancellationToken cancellationToken = default)
    {
        var options = new List<DownloadOption>();

        if (video.QualityUrls != null)
        {
            foreach (var kv in video.QualityUrls)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    options.Add(new DownloadOption
                    {
                        Quality = kv.Key,
                        Format = "mp4",
                        SourceUrl = kv.Value,
                        Size = null
                    });
                }
            }
        }

        if (options.Count == 0 && !string.IsNullOrEmpty(video.PlaybackUrl))
        {
            options.Add(new DownloadOption
            {
                Quality = "unknown",
                Format = "mp4",
                SourceUrl = video.PlaybackUrl,
                Size = null
            });
        }

        return Task.FromResult<IReadOnlyList<DownloadOption>>(options);
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadOption option,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var tempPath = destinationPath + ".part";

        return await _engine.DownloadAsync(
            option.SourceUrl ?? string.Empty,
            destinationPath,
            tempPath,
            option.Size,
            0,
            progress,
            cancellationToken);
    }
}
