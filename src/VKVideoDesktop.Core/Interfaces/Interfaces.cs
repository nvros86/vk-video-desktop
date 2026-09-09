using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Core.Interfaces;

public interface IVideoProvider
{
    Task<IReadOnlyList<Video>> SearchAsync(
        string query,
        SearchFilter filter,
        SearchSortOrder sortOrder,
        CancellationToken cancellationToken);

    Task<Video?> GetVideoAsync(
        string videoId,
        CancellationToken cancellationToken);

    Task<Channel?> GetChannelAsync(
        string channelId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> GetRecommendationsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> GetPopularAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> GetVideosByChannelAsync(
        string channelId,
        CancellationToken cancellationToken);
}

public interface IVideoDownloadProvider
{
    Task<IReadOnlyList<DownloadOption>> GetAvailableDownloadsAsync(
        Video video,
        CancellationToken cancellationToken);

    Task<DownloadResult> DownloadAsync(
        DownloadOption option,
        string destinationPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}

public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    Task<bool> LoginAsync(CancellationToken cancellationToken);
    Task LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}

public interface IHistoryService
{
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync();
    Task<HistoryEntry?> GetByVideoIdAsync(string videoId);
    Task SaveOrUpdateAsync(HistoryEntry entry);
    Task DeleteAsync(string entryId);
    Task ClearAsync();
}

public interface IFavoritesService
{
    Task<IReadOnlyList<FavoriteEntry>> GetAllAsync();
    Task<bool> IsFavoriteAsync(string videoId);
    Task AddAsync(FavoriteEntry entry);
    Task RemoveAsync(string videoId);
}

public interface IPlaylistService
{
    Task<IReadOnlyList<Playlist>> GetAllAsync();
    Task<Playlist?> GetByIdAsync(string playlistId);
    Task<Playlist> CreateAsync(string title, string? description);
    Task RenameAsync(string playlistId, string newTitle);
    Task DeleteAsync(string playlistId);
    Task AddVideoAsync(string playlistId, Video video);
    Task RemoveVideoAsync(string playlistId, string videoId);
    Task ReorderVideoAsync(string playlistId, string videoId, int newIndex);
}

public interface ISettingsService
{
    UserSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    Task ResetAsync();
}
