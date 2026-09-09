using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class DownloadItemViewModel : ViewModelBase
{
    private string _downloadId = string.Empty;
    private string _title = string.Empty;
    private string _thumbnailUrl = string.Empty;
    private string _statusText = string.Empty;
    private string _progressText = string.Empty;
    private double _progress;
    private DownloadStatus _status;

    public string DownloadId { get => _downloadId; set => SetProperty(ref _downloadId, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string ThumbnailUrl { get => _thumbnailUrl; set => SetProperty(ref _thumbnailUrl, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string ProgressText { get => _progressText; set => SetProperty(ref _progressText, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public DownloadStatus Status { get => _status; set => SetProperty(ref _status, value); }

    public void UpdateFrom(DownloadTask task)
    {
        DownloadId = task.Id;
        Title = task.Title;
        Status = task.Status;
        StatusText = GetStatusText(task.Status);
        Progress = task.Progress;
        ProgressText = task.Status == DownloadStatus.Downloading
            ? $"{task.DownloadedBytes / 1024.0 / 1024.0:F1} MB / {task.TotalBytes / 1024.0 / 1024.0:F1} MB"
            : StatusText;
    }

    private static string GetStatusText(DownloadStatus status) => status switch
    {
        DownloadStatus.Queued => "В очереди",
        DownloadStatus.Resolving => "Определение источника...",
        DownloadStatus.Downloading => "Загрузка...",
        DownloadStatus.Paused => "Приостановлено",
        DownloadStatus.Completed => "Завершено",
        DownloadStatus.Failed => "Ошибка",
        DownloadStatus.Cancelled => "Отменено",
        DownloadStatus.Retrying => "Повторная попытка...",
        _ => "Неизвестно"
    };
}
