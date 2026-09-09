namespace VKVideoDesktop.Core.Enums;

public enum VideoQuality
{
    Unknown = 0,
    P240 = 240,
    P360 = 360,
    P480 = 480,
    P720 = 720,
    P1080 = 1080,
    P1440 = 1440,
    P2160 = 2160
}

public enum DownloadStatus
{
    Queued = 0,
    Resolving = 1,
    Downloading = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6,
    Retrying = 7
}

public enum SearchFilter
{
    All = 0,
    Videos = 1,
    Channels = 2,
    Authors = 3,
    LiveStreams = 4
}

public enum SearchSortOrder
{
    Relevance = 0,
    Date = 1,
    Popularity = 2
}

public enum AppTheme
{
    Dark = 0,
    Light = 1,
    System = 2
}

public enum NavigationItemId
{
    Home = 0,
    Search = 1,
    MyVideos = 2,
    Subscriptions = 3,
    Favorites = 4,
    Playlists = 5,
    History = 6,
    Downloads = 7,
    Settings = 8
}

public enum DownloadSpeedLimit
{
    None = 0,
    KB512 = 512 * 1024,
    MB1 = 1 * 1024 * 1024,
    MB2 = 2 * 1024 * 1024,
    MB5 = 5 * 1024 * 1024,
    MB10 = 10 * 1024 * 1024,
    MB20 = 20 * 1024 * 1024
}

public enum DownloadQualityBehavior
{
    Ask = 0,
    UseLastSelected = 1,
    UseMaxAvailable = 2
}

public enum ErrorType
{
    NetworkError,
    AuthenticationError,
    AccessDenied,
    VideoUnavailable,
    DownloadUnavailable,
    RateLimited,
    ServerError,
    StorageError,
    InsufficientSpace,
    Timeout,
    RangeNotSupported,
    Cancelled,
    UnknownError
}
