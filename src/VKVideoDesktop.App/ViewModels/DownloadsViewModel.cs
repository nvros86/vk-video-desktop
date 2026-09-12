using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class DownloadsViewModel : ViewModelBase
{
    private readonly DownloadService _downloadService;
    private readonly IDownloadRepository _downloadRepository;
    private readonly ILogger<DownloadsViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private bool _hasActiveDownloads;

    public DownloadsViewModel(
        DownloadService downloadService,
        IDownloadRepository downloadRepository,
        ILogger<DownloadsViewModel> logger)
    {
        _downloadService = downloadService;
        _downloadRepository = downloadRepository;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _downloadService.DownloadProgressChanged += OnProgressChanged;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;
    }

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();

    public bool HasActiveDownloads
    {
        get => _hasActiveDownloads;
        set => SetProperty(ref _hasActiveDownloads, value);
    }

    public int ActiveDownloadsCount => Downloads.Count(d =>
        d.Status == DownloadStatus.Downloading ||
        d.Status == DownloadStatus.Queued ||
        d.Status == DownloadStatus.Resolving ||
        d.Status == DownloadStatus.Retrying);

    public async Task LoadDownloadsAsync()
    {
        var tasks = await _downloadRepository.GetAllAsync();
        Downloads.Clear();
        foreach (var task in tasks.Where(t => t.Status != DownloadStatus.Completed))
        {
            var item = new DownloadItemViewModel();
            item.UpdateFrom(task);
            Downloads.Add(item);
        }
        OnPropertyChanged(nameof(ActiveDownloadsCount));
        HasActiveDownloads = ActiveDownloadsCount > 0;
    }

    public async Task PauseAsync(string downloadId)
    {
        await _downloadService.PauseAsync(downloadId);
    }

    public async Task ResumeAsync(string downloadId)
    {
        await _downloadService.ResumeAsync(downloadId);
    }

    public async Task CancelAsync(string downloadId)
    {
        await _downloadService.CancelAsync(downloadId);
        var item = Downloads.FirstOrDefault(d => d.DownloadId == downloadId);
        if (item != null) Downloads.Remove(item);
    }

    public async Task RemoveDownloadAsync(string downloadId)
    {
        try
        {
            await _downloadService.RemoveAsync(downloadId, deleteFile: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove download {DownloadId}", downloadId);
        }

        var item = Downloads.FirstOrDefault(d => d.DownloadId == downloadId);
        if (item != null) Downloads.Remove(item);
    }

    public async Task ClearCompletedAsync()
    {
        var completed = Downloads.Where(d => d.Status == DownloadStatus.Completed).ToList();
        foreach (var item in completed)
        {
            try
            {
                await _downloadService.RemoveAsync(item.DownloadId, deleteFile: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove completed download {DownloadId}", item.DownloadId);
            }

            Downloads.Remove(item);
        }
    }

    public async Task RetryAllFailedAsync()
    {
        var failed = Downloads.Where(d => d.Status == DownloadStatus.Failed).ToList();
        foreach (var item in failed)
        {
            await _downloadService.RetryAsync(item.DownloadId);
        }
    }

    private void OnProgressChanged(object? sender, DownloadTask task)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
            if (item != null)
            {
                item.UpdateFrom(task);
            }
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadTask task)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
            if (item != null)
            {
                item.UpdateFrom(task);
            }
            OnPropertyChanged(nameof(ActiveDownloadsCount));
            HasActiveDownloads = ActiveDownloadsCount > 0;
        });
    }

    private void OnDownloadFailed(object? sender, DownloadTask task)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
            if (item != null)
            {
                item.UpdateFrom(task);
            }
            OnPropertyChanged(nameof(ActiveDownloadsCount));
            HasActiveDownloads = ActiveDownloadsCount > 0;
        });
    }
}
