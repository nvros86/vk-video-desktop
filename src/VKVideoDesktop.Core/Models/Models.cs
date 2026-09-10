using VKVideoDesktop.Core.Enums;

namespace VKVideoDesktop.Core.Models;

public sealed class Video
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string? ChannelId { get; init; }
    public string? ChannelName { get; init; }
    public string? ChannelAvatarUrl { get; init; }
    public TimeSpan Duration { get; init; }
    public long ViewCount { get; init; }
    public DateTime PublishedAt { get; init; }
    public string? Url { get; init; }
    public bool IsLive { get; init; }
    public string? PlaybackUrl { get; init; }
    public Dictionary<string, string>? QualityUrls { get; init; }
}

public sealed class Channel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string AvatarUrl { get; init; } = string.Empty;
    public string? BannerUrl { get; init; }
    public string Description { get; init; } = string.Empty;
    public long SubscriberCount { get; init; }
    public long VideoCount { get; init; }
    public bool IsSubscribed { get; set; }
}

public sealed record Playlist
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int VideoCount { get; init; }
    public List<Video> Videos { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public sealed class HistoryEntry
{
    public string Id { get; init; } = string.Empty;
    public string VideoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public TimeSpan LastPosition { get; set; }
    public DateTime LastViewed { get; set; }
}

public sealed class FavoriteEntry
{
    public string Id { get; init; } = string.Empty;
    public string VideoId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public DateTime AddedAt { get; init; }
}

public sealed class SearchResult
{
    public IReadOnlyList<Video> Videos { get; init; } = Array.Empty<Video>();
    public IReadOnlyList<Channel> Channels { get; init; } = Array.Empty<Channel>();
    public bool HasMore { get; init; }
    public string? NextPageToken { get; init; }
}

public sealed class PlaybackState
{
    public string? CurrentVideoId { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsPlaying { get; set; }
    public double Volume { get; set; } = 1.0;
    public bool IsMuted { get; set; }
    public double PlaybackSpeed { get; set; } = 1.0;
    public bool IsFullscreen { get; set; }
    public List<string> Queue { get; init; } = new();
    public int CurrentQueueIndex { get; set; } = -1;
}

public sealed class UserSettings
{
    public string Language { get; set; } = "ru";
    public AppTheme Theme { get; set; } = AppTheme.Dark;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public string DefaultQuality { get; set; } = "1080p";
    public bool Autoplay { get; set; } = true;
    public double DefaultVolume { get; set; } = 1.0;
    public double DefaultPlaybackSpeed { get; set; } = 1.0;
    public string DownloadFolder { get; set; } = string.Empty;
    public int MaxConcurrentDownloads { get; set; } = 2;
    public long SpeedLimit { get; set; }
    public int RetryCount { get; set; } = 3;
    public bool AutoResumeAfterStartup { get; set; } = true;
    public bool DeletePartOnCancel { get; set; } = true;
    public bool AskFolderBeforeDownload { get; set; }
    public DownloadQualityBehavior DownloadQualityBehavior { get; set; } = DownloadQualityBehavior.Ask;
    public bool UseProxy { get; set; }
    public string ProxyAddress { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public string? AccessToken { get; set; }
    public UserSettings()
    {
        DownloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Videos", "VK Video");
    }
}
