using Microsoft.Extensions.DependencyInjection;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class DownloadItemViewModel : ViewModelBase
{
    private string _downloadId = string.Empty;
    private string _title = string.Empty;
    private string _thumbnailUrl = string.Empty;
    private string _destinationPath = string.Empty;
    private string _statusText = string.Empty;
    private string _progressText = string.Empty;
    private string _speedText = string.Empty;
    private string _etaText = string.Empty;
    private double _progress;
    private DownloadStatus _status;

    public string DownloadId { get => _downloadId; set => SetProperty(ref _downloadId, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string ThumbnailUrl { get => _thumbnailUrl; set => SetProperty(ref _thumbnailUrl, value); }
    public string DestinationPath { get => _destinationPath; set => SetProperty(ref _destinationPath, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string ProgressText { get => _progressText; set => SetProperty(ref _progressText, value); }
    public string SpeedText { get => _speedText; set => SetProperty(ref _speedText, value); }
    public string EtaText { get => _etaText; set => SetProperty(ref _etaText, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public DownloadStatus Status { get => _status; set => SetProperty(ref _status, value); }

    public bool IsDownloading => Status == DownloadStatus.Downloading || Status == DownloadStatus.Queued || Status == DownloadStatus.Resolving || Status == DownloadStatus.Retrying;
    public bool IsPaused => Status == DownloadStatus.Paused;

    public void UpdateFrom(DownloadTask task)
    {
        DownloadId = task.Id;
        Title = task.Title;
        ThumbnailUrl = task.ThumbnailUrl;
        DestinationPath = task.DestinationPath;
        Status = task.Status;
        StatusText = GetStatusText(task.Status);
        Progress = task.Progress;

        if (task.Status == DownloadStatus.Downloading)
        {
            var downloaded = task.DownloadedBytes / 1024.0 / 1024.0;
            var total = task.TotalBytes / 1024.0 / 1024.0;
            ProgressText = $"{downloaded:F1} MB / {total:F1} MB";

            if (task.SpeedBytesPerSecond > 0)
            {
                var speed = task.SpeedBytesPerSecond / 1024.0 / 1024.0;
                SpeedText = $"{speed:F1} MB/s";
            }

            if (task.RemainingTime.HasValue && task.RemainingTime.Value.TotalSeconds > 0)
            {
                var remaining = task.RemainingTime.Value;
                var loc = App.Services.GetRequiredService<LocalizationService>();
                EtaText = remaining.TotalHours >= 1
                    ? $"~{string.Format(loc["TimeHoursMinutes"], (int)remaining.TotalHours, remaining.Minutes)}"
                    : $"~{string.Format(loc["TimeMinutesSeconds"], (int)remaining.TotalMinutes, remaining.Seconds)}";
            }
        }
        else
        {
            ProgressText = StatusText;
            SpeedText = string.Empty;
            EtaText = string.Empty;
        }
    }

    private static string GetStatusText(DownloadStatus status)
    {
        var loc = App.Services.GetRequiredService<LocalizationService>();
        return status switch
        {
            DownloadStatus.Queued => loc["DownloadStatusQueued"],
            DownloadStatus.Resolving => loc["DownloadStatusResolving"],
            DownloadStatus.Downloading => loc["DownloadStatusDownloading"],
            DownloadStatus.Paused => loc["DownloadStatusPaused"],
            DownloadStatus.Completed => loc["DownloadStatusCompleted"],
            DownloadStatus.Failed => loc["DownloadStatusFailed"],
            DownloadStatus.Cancelled => loc["DownloadStatusCancelled"],
            DownloadStatus.Retrying => loc["DownloadStatusRetrying"],
            _ => loc["DownloadStatusUnknown"]
        };
    }
}
