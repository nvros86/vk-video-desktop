using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class VkVideoDownloadProvider : IVideoDownloadProvider
{
    private readonly IDownloadSourceResolver _resolver;
    private readonly IDownloadEngine _engine;

    public VkVideoDownloadProvider(IDownloadSourceResolver resolver, IDownloadEngine engine)
    {
        _resolver = resolver;
        _engine = engine;
    }

    public async Task<IReadOnlyList<DownloadOption>> GetAvailableDownloadsAsync(
        Video video,
        CancellationToken cancellationToken = default)
    {
        return await _resolver.ResolveAsync(video, cancellationToken);
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
