using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class DownloadManager : IDownloadManager
{
    private readonly IDownloadEngine _engine;
    private readonly IDownloadSourceResolver _sourceResolver;
    private readonly IDownloadRepository _repository;
    private readonly ILogger<DownloadManager> _logger;
    private readonly ConcurrentDictionary<string, DownloadTask> _downloads = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly SemaphoreSlim _concurrencySemaphore;

    public DownloadManager(
        IDownloadEngine engine,
        IDownloadSourceResolver sourceResolver,
        IDownloadRepository repository,
        ILogger<DownloadManager> logger,
        int maxConcurrent = 2)
    {
        _engine = engine;
        _sourceResolver = sourceResolver;
        _repository = repository;
        _logger = logger;
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public IReadOnlyList<DownloadTask> Downloads =>
        _downloads.Values.OrderByDescending(d => d.CreatedAt).ToList();

    public int ActiveDownloadsCount =>
        _downloads.Values.Count(d =>
            d.Status == DownloadStatus.Downloading || d.Status == DownloadStatus.Resolving);

    public event EventHandler<DownloadTask>? DownloadProgressChanged;
    public event EventHandler<DownloadTask>? DownloadCompleted;
    public event EventHandler<DownloadTask>? DownloadFailed;

    public async Task<DownloadTask> AddAsync(DownloadRequest request, CancellationToken cancellationToken)
    {
        var temporaryPath = request.DestinationPath + ".part";

        var task = new DownloadTask
        {
            VideoId = request.VideoId,
            Title = request.Title,
            ThumbnailUrl = request.ThumbnailUrl,
            SourceUrl = request.Option.SourceUrl ?? string.Empty,
            DestinationPath = request.DestinationPath,
            TemporaryPath = temporaryPath,
            Quality = request.Option.Quality,
            Format = request.Option.Format,
            TotalBytes = request.Option.Size,
            Status = DownloadStatus.Queued
        };

        _downloads[task.Id] = task;
        await _repository.SaveAsync(task);

        _ = ProcessDownloadAsync(task.Id);

        return task;
    }

    public async Task PauseAsync(string downloadId, CancellationToken cancellationToken)
    {
        if (_downloads.TryGetValue(downloadId, out var task) &&
            task.Status == DownloadStatus.Downloading)
        {
            if (_cancellations.TryRemove(downloadId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            task.Status = DownloadStatus.Paused;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Paused);
            DownloadProgressChanged?.Invoke(this, task);
        }
    }

    public async Task ResumeAsync(string downloadId, CancellationToken cancellationToken)
    {
        if (_downloads.TryGetValue(downloadId, out var task) &&
            task.Status == DownloadStatus.Paused)
        {
            task.Status = DownloadStatus.Queued;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Queued);
            _ = ProcessDownloadAsync(downloadId);
        }
    }

    public async Task CancelAsync(string downloadId, CancellationToken cancellationToken)
    {
        if (_downloads.TryGetValue(downloadId, out var task))
        {
            if (_cancellations.TryRemove(downloadId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            task.Status = DownloadStatus.Cancelled;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Cancelled);

            if (File.Exists(task.TemporaryPath))
                File.Delete(task.TemporaryPath);

            DownloadProgressChanged?.Invoke(this, task);
        }
    }

    public async Task RetryAsync(string downloadId, CancellationToken cancellationToken)
    {
        if (_downloads.TryGetValue(downloadId, out var task) &&
            (task.Status == DownloadStatus.Failed || task.Status == DownloadStatus.Cancelled))
        {
            task.Status = DownloadStatus.Queued;
            task.RetryCount++;
            task.ErrorMessage = null;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Queued);
            _ = ProcessDownloadAsync(downloadId);
        }
    }

    public async Task RemoveAsync(string downloadId, bool deleteFile, CancellationToken cancellationToken)
    {
        if (_downloads.TryRemove(downloadId, out var task))
        {
            if (_cancellations.TryRemove(downloadId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (deleteFile && File.Exists(task.TemporaryPath))
                File.Delete(task.TemporaryPath);

            if (deleteFile && File.Exists(task.DestinationPath))
                File.Delete(task.DestinationPath);

            await _repository.DeleteAsync(downloadId);
        }
    }

    private async Task ProcessDownloadAsync(string downloadId)
    {
        await _concurrencySemaphore.WaitAsync();

        try
        {
            if (!_downloads.TryGetValue(downloadId, out var task))
                return;

            var cts = new CancellationTokenSource();
            _cancellations[downloadId] = cts;

            task.Status = DownloadStatus.Resolving;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Resolving);
            DownloadProgressChanged?.Invoke(this, task);

            if (string.IsNullOrEmpty(task.SourceUrl))
            {
                var options = await _sourceResolver.ResolveAsync(
                    new Video { Id = task.VideoId, Title = task.Title },
                    cts.Token);

                var matching = options.FirstOrDefault(o =>
                    o.Quality == task.Quality && o.Format == task.Format);

                if (matching?.SourceUrl == null)
                {
                    task.Status = DownloadStatus.Failed;
                    task.ErrorMessage = "Could not resolve download source";
                    await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Failed);
                    DownloadFailed?.Invoke(this, task);
                    return;
                }

                task.SourceUrl = matching.SourceUrl;
            }

            task.Status = DownloadStatus.Downloading;
            task.StartedAt = DateTime.UtcNow;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Downloading);
            DownloadProgressChanged?.Invoke(this, task);

            var progress = new Progress<DownloadProgress>(p =>
            {
                task.DownloadedBytes = p.BytesReceived;
                task.TotalBytes = p.TotalBytes;
                task.Progress = p.ProgressPercent;
                task.SpeedBytesPerSecond = p.SpeedBytesPerSecond;
                task.RemainingTime = p.RemainingTime;
                DownloadProgressChanged?.Invoke(this, task);
            });

            var result = await _engine.DownloadAsync(
                task.SourceUrl,
                task.DestinationPath,
                task.TemporaryPath,
                task.TotalBytes,
                progress,
                cts.Token);

            if (result.IsSuccess)
            {
                if (File.Exists(task.DestinationPath))
                    File.Delete(task.DestinationPath);

                File.Move(task.TemporaryPath, task.DestinationPath);

                task.Status = DownloadStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.Progress = 100;
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Completed);
                DownloadCompleted?.Invoke(this, task);
            }
            else if (result.ErrorType == ErrorType.Cancelled)
            {
                task.Status = DownloadStatus.Cancelled;
            }
            else if (task.RetryCount < 3)
            {
                task.Status = DownloadStatus.Retrying;
                task.RetryCount++;
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Retrying);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, task.RetryCount)), cts.Token);
                _ = ProcessDownloadAsync(downloadId);
                return;
            }
            else
            {
                task.Status = DownloadStatus.Failed;
                task.ErrorMessage = result.ErrorMessage;
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Failed);
                DownloadFailed?.Invoke(this, task);
            }
        }
        catch (OperationCanceledException)
        {
            // Download was cancelled - status already updated
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing download {DownloadId}", downloadId);

            if (_downloads.TryGetValue(downloadId, out var task))
            {
                task.Status = DownloadStatus.Failed;
                task.ErrorMessage = ex.Message;
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Failed);
                DownloadFailed?.Invoke(this, task);
            }
        }
        finally
        {
            _concurrencySemaphore.Release();
        }
    }
}
