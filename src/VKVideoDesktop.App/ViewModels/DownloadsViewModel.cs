using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class DownloadsViewModel : ViewModelBase
{
    private readonly DownloadService _downloadService;
    private readonly ILogger<DownloadsViewModel> _logger;

    public DownloadsViewModel(DownloadService downloadService, ILogger<DownloadsViewModel> logger)
    {
        _downloadService = downloadService;
        _logger = logger;

        _downloadService.DownloadProgressChanged += OnProgressChanged;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;
    }

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = new();

    public async Task LoadDownloadsAsync()
    {
        var repository = App.GetService<IDownloadRepository>();
        var tasks = await repository.GetAllAsync();
        Downloads.Clear();
        foreach (var task in tasks.Where(t => t.Status != DownloadStatus.Completed))
        {
            var item = new DownloadItemViewModel();
            item.UpdateFrom(task);
            Downloads.Add(item);
        }
    }

    public string ActiveDownloadsText =>
        $"{_downloadService.ActiveDownloadsCount} активных";

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
    }

    public async Task ClearCompletedAsync()
    {
        var completed = Downloads.Where(d => d.Status == DownloadStatus.Completed).ToList();
        foreach (var item in completed)
        {
            Downloads.Remove(item);
        }
    }

    private void OnProgressChanged(object? sender, DownloadTask task)
    {
        var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
        if (item != null)
        {
            item.UpdateFrom(task);
        }
    }

    private void OnDownloadCompleted(object? sender, DownloadTask task)
    {
        var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
        if (item != null)
        {
            item.UpdateFrom(task);
        }
    }

    private void OnDownloadFailed(object? sender, DownloadTask task)
    {
        var item = Downloads.FirstOrDefault(d => d.DownloadId == task.Id);
        if (item != null)
        {
            item.UpdateFrom(task);
        }
    }
}
