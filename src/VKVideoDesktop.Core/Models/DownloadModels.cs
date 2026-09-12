using VKVideoDesktop.Core.Enums;

namespace VKVideoDesktop.Core.Models;

public sealed class DownloadTask
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string VideoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string TemporaryPath { get; init; } = string.Empty;
    public string? Quality { get; init; }
    public string? Format { get; init; }
    public long? TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double Progress { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan? RemainingTime { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public sealed class DownloadOption
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string VideoId { get; init; } = string.Empty;
    public string Quality { get; init; } = string.Empty;
    public string Format { get; init; } = "mp4";
    public long? Size { get; init; }
    public bool IsAvailable { get; init; } = true;
    public string? Label { get; init; }
    public string? SourceUrl { get; init; }
}

public sealed class DownloadRequest
{
    public string VideoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public DownloadOption Option { get; init; } = null!;
    public string DestinationPath { get; init; } = string.Empty;
}

public sealed class DownloadResult
{
    public bool IsSuccess { get; init; }
    public string? FilePath { get; init; }
    public string? ErrorMessage { get; init; }
    public ErrorType? ErrorType { get; init; }
    public long BytesWritten { get; init; }
}
