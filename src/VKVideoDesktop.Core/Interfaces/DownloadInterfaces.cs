using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Core.Interfaces;

public interface IDownloadManager
{
    IReadOnlyList<DownloadTask> Downloads { get; }
    Task<DownloadTask> AddAsync(
        DownloadRequest request,
        CancellationToken cancellationToken);
    Task PauseAsync(
        string downloadId,
        CancellationToken cancellationToken);
    Task ResumeAsync(
        string downloadId,
        CancellationToken cancellationToken);
    Task CancelAsync(
        string downloadId,
        CancellationToken cancellationToken);
    Task RetryAsync(
        string downloadId,
        CancellationToken cancellationToken);
    Task RemoveAsync(
        string downloadId,
        bool deleteFile,
        CancellationToken cancellationToken);
    int ActiveDownloadsCount { get; }
    event EventHandler<DownloadTask>? DownloadProgressChanged;
    event EventHandler<DownloadTask>? DownloadCompleted;
    event EventHandler<DownloadTask>? DownloadFailed;
}

public interface IDownloadEngine
{
    Task<DownloadResult> DownloadAsync(
        string sourceUrl,
        string destinationPath,
        string temporaryPath,
        long? totalBytes,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}

public interface IDownloadSourceResolver
{
    Task<IReadOnlyList<DownloadOption>> ResolveAsync(
        Video video,
        CancellationToken cancellationToken);
}

public interface IFileWriter
{
    Task WriteAsync(
        string filePath,
        Stream dataStream,
        IProgress<long>? progress,
        CancellationToken cancellationToken);
}

public interface IDownloadQueue
{
    IReadOnlyList<DownloadTask> QueuedTasks { get; }
    Task EnqueueAsync(DownloadTask task);
    Task<DownloadTask?> DequeueAsync();
    Task RemoveAsync(string downloadId);
    Task ClearAsync();
}

public interface IDownloadRepository
{
    Task<IReadOnlyList<DownloadTask>> GetAllAsync();
    Task<DownloadTask?> GetByIdAsync(string id);
    Task SaveAsync(DownloadTask task);
    Task DeleteAsync(string id);
    Task UpdateStatusAsync(string id, DownloadStatus status);
    Task UpdateProgressAsync(string id, long downloadedBytes, double progress);
}

public interface IThumbnailCache
{
    Task<string?> GetCachedPathAsync(string url);
    Task<string> CacheAsync(string url, CancellationToken cancellationToken);
    Task ClearCacheAsync();
}

public sealed class DownloadProgress
{
    public long BytesReceived { get; init; }
    public long? TotalBytes { get; init; }
    public double ProgressPercent { get; init; }
    public double SpeedBytesPerSecond { get; init; }
    public TimeSpan? RemainingTime { get; init; }
}
