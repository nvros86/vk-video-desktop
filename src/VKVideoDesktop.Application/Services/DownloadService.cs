using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class DownloadService
{
    private readonly IDownloadManager _downloadManager;
    private readonly IVideoDownloadProvider _downloadProvider;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DownloadService> _logger;

    public DownloadService(
        IDownloadManager downloadManager,
        IVideoDownloadProvider downloadProvider,
        ISettingsService settingsService,
        ILogger<DownloadService> logger)
    {
        _downloadManager = downloadManager;
        _downloadProvider = downloadProvider;
        _settingsService = settingsService;
        _logger = logger;
    }

    public IReadOnlyList<DownloadTask> Downloads => _downloadManager.Downloads;

    public int ActiveDownloadsCount => _downloadManager.ActiveDownloadsCount;

    public event EventHandler<DownloadTask>? DownloadProgressChanged
    {
        add => _downloadManager.DownloadProgressChanged += value;
        remove => _downloadManager.DownloadProgressChanged -= value;
    }

    public event EventHandler<DownloadTask>? DownloadCompleted
    {
        add => _downloadManager.DownloadCompleted += value;
        remove => _downloadManager.DownloadCompleted -= value;
    }

    public event EventHandler<DownloadTask>? DownloadFailed
    {
        add => _downloadManager.DownloadFailed += value;
        remove => _downloadManager.DownloadFailed -= value;
    }

    public async Task<IReadOnlyList<DownloadOption>> GetAvailableDownloadsAsync(
        Video video,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _downloadProvider.GetAvailableDownloadsAsync(video, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve download options for {VideoId}", video.Id);
            return Array.Empty<DownloadOption>();
        }
    }

    public async Task<DownloadTask?> StartDownloadAsync(
        Video video,
        DownloadOption option,
        CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.Settings;

        var destinationPath = Path.Combine(
            settings.DownloadFolder,
            SanitizeFileName(video.Title) + " [" + video.Id + "]." + option.Format);

        destinationPath = GetUniqueFilePath(destinationPath);

        var request = new DownloadRequest
        {
            VideoId = video.Id,
            Title = video.Title,
            ThumbnailUrl = video.ThumbnailUrl,
            Option = option,
            DestinationPath = destinationPath
        };

        try
        {
            _logger.LogInformation(
                "Starting download: {Title} ({Quality})",
                video.Title, option.Quality);

            return await _downloadManager.AddAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start download for {VideoId}", video.Id);
            return null;
        }
    }

    public async Task PauseAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        await _downloadManager.PauseAsync(downloadId, cancellationToken);
    }

    public async Task ResumeAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        await _downloadManager.ResumeAsync(downloadId, cancellationToken);
    }

    public async Task CancelAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        await _downloadManager.CancelAsync(downloadId, cancellationToken);
    }

    public async Task RetryAsync(string downloadId, CancellationToken cancellationToken = default)
    {
        await _downloadManager.RetryAsync(downloadId, cancellationToken);
    }

    public async Task RemoveAsync(
        string downloadId,
        bool deleteFile,
        CancellationToken cancellationToken = default)
    {
        await _downloadManager.RemoveAsync(downloadId, deleteFile, cancellationToken);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
    }

    private static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{nameWithoutExt} ({counter}){extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }
}
