using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Metrics;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class DownloadManager : IDownloadManager
{
    private readonly IDownloadEngine _engine;
    private readonly IVideoDownloadProvider _downloadProvider;
    private readonly IDownloadRepository _repository;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DownloadManager> _logger;
    private readonly AppMetrics _metrics;
    private readonly ConcurrentDictionary<string, DownloadTask> _downloads = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private SemaphoreSlim _concurrencySemaphore;

    public DownloadManager(
        IDownloadEngine engine,
        IVideoDownloadProvider downloadProvider,
        IDownloadRepository repository,
        ISettingsService settingsService,
        ILogger<DownloadManager> logger,
        AppMetrics metrics)
    {
        _engine = engine;
        _downloadProvider = downloadProvider;
        _repository = repository;
        _settingsService = settingsService;
        _logger = logger;
        _metrics = metrics;
        _concurrencySemaphore = new SemaphoreSlim(
            _settingsService.Settings.MaxConcurrentDownloads,
            _settingsService.Settings.MaxConcurrentDownloads);
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
            (task.Status == DownloadStatus.Downloading || task.Status == DownloadStatus.Queued || task.Status == DownloadStatus.Resolving))
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

            if (_settingsService.Settings.DeletePartOnCancel && File.Exists(task.TemporaryPath))
            {
                try { File.Delete(task.TemporaryPath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete .part file for {DownloadId}", downloadId); }
            }

            DownloadFailed?.Invoke(this, task);
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

    public async Task RecoverIncompleteDownloadsAsync()
    {
        var allTasks = await _repository.GetAllAsync();
        var incomplete = allTasks.Where(t =>
            t.Status == DownloadStatus.Downloading ||
            t.Status == DownloadStatus.Resolving ||
            t.Status == DownloadStatus.Retrying);

        foreach (var task in incomplete)
        {
            task.Status = DownloadStatus.Queued;
            await _repository.UpdateStatusAsync(task.Id, DownloadStatus.Queued);

            if (_downloads.TryAdd(task.Id, task))
            {
                _ = ProcessDownloadAsync(task.Id);
            }
        }
    }

    private async Task ProcessDownloadAsync(string downloadId)
    {
        await _concurrencySemaphore.WaitAsync();

        try
        {
            if (!_downloads.TryGetValue(downloadId, out var task))
                return;

            using var logScope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["DownloadId"] = task.Id,
                ["VideoId"] = task.VideoId,
                ["Title"] = task.Title
            });

            _logger.LogInformation("Download processing started for {Title}", task.Title);

            var cts = new CancellationTokenSource();
            _cancellations[downloadId] = cts;

            task.Status = DownloadStatus.Resolving;
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Resolving);
            DownloadProgressChanged?.Invoke(this, task);

            if (string.IsNullOrEmpty(task.SourceUrl))
            {
                var options = await _downloadProvider.GetAvailableDownloadsAsync(
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
            _metrics.DownloadStarted();
            await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Downloading);
            DownloadProgressChanged?.Invoke(this, task);

            var lastProgressSave = DateTime.MinValue;
            var progress = new Progress<DownloadProgress>(p =>
            {
                task.DownloadedBytes = p.BytesReceived;
                task.TotalBytes = p.TotalBytes;
                task.Progress = p.ProgressPercent;
                task.SpeedBytesPerSecond = p.SpeedBytesPerSecond;
                task.RemainingTime = p.RemainingTime;

                if (DateTime.UtcNow - lastProgressSave > TimeSpan.FromMilliseconds(500))
                {
                    lastProgressSave = DateTime.UtcNow;
                    _ = _repository.UpdateProgressAsync(downloadId, p.BytesReceived, p.ProgressPercent);
                }

                DownloadProgressChanged?.Invoke(this, task);
            });

            var result = await _engine.DownloadAsync(
                task.SourceUrl,
                task.DestinationPath,
                task.TemporaryPath,
                task.TotalBytes,
                _settingsService.Settings.SpeedLimit,
                progress,
                cts.Token);

            if (result.IsSuccess)
            {
                task.Status = DownloadStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.Progress = 100;
                task.DestinationPath = result.FilePath ?? task.DestinationPath;
                var duration = task.CompletedAt - task.StartedAt;
                _metrics.DownloadCompleted();
                _logger.LogInformation("Download completed: {Title} in {Duration:N1}s, {Size:N0} bytes",
                    task.Title, duration?.TotalSeconds ?? 0, task.DownloadedBytes);
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Completed);
                DownloadCompleted?.Invoke(this, task);
            }
            else if (result.ErrorType == ErrorType.Cancelled)
            {
                task.Status = DownloadStatus.Cancelled;
                _metrics.DownloadCancelled();
                _logger.LogWarning("Download cancelled: {Title}", task.Title);
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Cancelled);
            }
            else if (task.RetryCount < _settingsService.Settings.RetryCount)
            {
                task.Status = DownloadStatus.Retrying;
                task.RetryCount++;
                _metrics.TrackRetry(result.ErrorMessage);
                _logger.LogWarning("Download retry {RetryCount}/{MaxRetries} for {Title}: {Error}",
                    task.RetryCount, _settingsService.Settings.RetryCount, task.Title, result.ErrorMessage);
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Retrying);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, task.RetryCount)), cts.Token);
                _ = ProcessDownloadAsync(downloadId);
                return;
            }
            else
            {
                task.Status = DownloadStatus.Failed;
                task.ErrorMessage = result.ErrorMessage;
                _metrics.DownloadFailed(result.ErrorMessage);
                _logger.LogError("Download failed after {RetryCount} retries: {Title} - {Error}",
                    task.RetryCount, task.Title, result.ErrorMessage);
                await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Failed);
                DownloadFailed?.Invoke(this, task);
            }
        }
        catch (OperationCanceledException)
        {
            if (_downloads.TryGetValue(downloadId, out var task))
            {
                if (task.Status != DownloadStatus.Paused && task.Status != DownloadStatus.Cancelled)
                {
                    task.Status = DownloadStatus.Failed;
                    task.ErrorMessage = "Download cancelled";
                    await _repository.UpdateStatusAsync(downloadId, DownloadStatus.Failed);
                }
            }
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

            if (_cancellations.TryRemove(downloadId, out var cts))
            {
                cts.Dispose();
            }
        }
    }
}
