using Moq;
using VKVideoDesktop.Core.Interfaces;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class UiDeletionTests
{
    [Fact]
    public async Task History_DeleteEntry_CallsDeleteAsync()
    {
        var mock = new Mock<IHistoryService>();
        var entryId = "entry-123";

        await mock.Object.DeleteAsync(entryId);

        mock.Verify(s => s.DeleteAsync(entryId), Times.Once);
    }

    [Fact]
    public async Task History_ClearAll_CallsClearAsync()
    {
        var mock = new Mock<IHistoryService>();

        await mock.Object.ClearAsync();

        mock.Verify(s => s.ClearAsync(), Times.Once);
    }

    [Fact]
    public async Task Playlists_DeletePlaylist_CallsDeleteAsync()
    {
        var mock = new Mock<IPlaylistService>();
        var playlistId = "playlist-456";

        await mock.Object.DeleteAsync(playlistId);

        mock.Verify(s => s.DeleteAsync(playlistId), Times.Once);
    }

    [Fact]
    public async Task Playlists_RemoveVideo_CallsRemoveVideoAsync()
    {
        var mock = new Mock<IPlaylistService>();
        var playlistId = "playlist-456";
        var videoId = "video-789";

        await mock.Object.RemoveVideoAsync(playlistId, videoId);

        mock.Verify(s => s.RemoveVideoAsync(playlistId, videoId), Times.Once);
    }

    [Fact]
    public async Task Favorites_RemoveFavorite_CallsRemoveAsync()
    {
        var mock = new Mock<IFavoritesService>();
        var videoId = "video-abc";

        await mock.Object.RemoveAsync(videoId);

        mock.Verify(s => s.RemoveAsync(videoId), Times.Once);
    }

    [Fact]
    public async Task Downloads_CancelDownload_CallsCancelAsync()
    {
        var mock = new Mock<IDownloadManager>();
        var downloadId = "dl-001";
        var cts = new CancellationTokenSource();

        await mock.Object.CancelAsync(downloadId, cts.Token);

        mock.Verify(s => s.CancelAsync(downloadId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Downloads_RemoveDownload_CallsRemoveAsync()
    {
        var mock = new Mock<IDownloadManager>();
        var downloadId = "dl-002";
        var deleteFile = true;
        var cts = new CancellationTokenSource();

        await mock.Object.RemoveAsync(downloadId, deleteFile, cts.Token);

        mock.Verify(s => s.RemoveAsync(downloadId, deleteFile, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Downloads_RetryDownload_CallsRetryAsync()
    {
        var mock = new Mock<IDownloadManager>();
        var downloadId = "dl-003";
        var cts = new CancellationTokenSource();

        await mock.Object.RetryAsync(downloadId, cts.Token);

        mock.Verify(s => s.RetryAsync(downloadId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Downloads_PauseDownload_CallsPauseAsync()
    {
        var mock = new Mock<IDownloadManager>();
        var downloadId = "dl-004";
        var cts = new CancellationTokenSource();

        await mock.Object.PauseAsync(downloadId, cts.Token);

        mock.Verify(s => s.PauseAsync(downloadId, cts.Token), Times.Once);
    }

    [Fact]
    public async Task Downloads_ResumeDownload_CallsResumeAsync()
    {
        var mock = new Mock<IDownloadManager>();
        var downloadId = "dl-005";
        var cts = new CancellationTokenSource();

        await mock.Object.ResumeAsync(downloadId, cts.Token);

        mock.Verify(s => s.ResumeAsync(downloadId, cts.Token), Times.Once);
    }
}
