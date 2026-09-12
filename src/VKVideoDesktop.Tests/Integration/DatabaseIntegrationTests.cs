using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Data.Database;
using Xunit;

namespace VKVideoDesktop.Tests.Integration;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly string _dbPath;

    public DatabaseIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { }
    }

    private string MakeConnectionString() => $"Data Source={_dbPath}";

    [Fact]
    public async Task HistoryDatabase_CRUD()
    {
        var db = new HistoryDatabase(MakeConnectionString());

        var entry = new HistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "v123",
            Title = "Test Video",
            Author = "Author",
            ThumbnailUrl = "https://thumb.url",
            Duration = TimeSpan.FromMinutes(5),
            LastPosition = TimeSpan.FromMinutes(1),
            LastViewed = DateTime.UtcNow
        };

        await db.SaveOrUpdateAsync(entry);

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("v123", all[0].VideoId);
        Assert.Equal("Test Video", all[0].Title);

        var byVideoId = await db.GetByVideoIdAsync("v123");
        Assert.NotNull(byVideoId);
        Assert.Equal(entry.Id, byVideoId!.Id);

        var notFound = await db.GetByVideoIdAsync("nonexistent");
        Assert.Null(notFound);

        await db.DeleteAsync(entry.Id);
        var afterDelete = await db.GetAllAsync();
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task HistoryDatabase_SaveOrUpdate_UpdatesExisting()
    {
        var db = new HistoryDatabase(MakeConnectionString());

        var entry = new HistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "v1",
            Title = "Original",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(10),
            LastPosition = TimeSpan.Zero,
            LastViewed = DateTime.UtcNow
        };

        await db.SaveOrUpdateAsync(entry);

        var updatedEntry = new HistoryEntry
        {
            Id = entry.Id,
            VideoId = entry.VideoId,
            Title = "Updated",
            Author = entry.Author,
            Duration = entry.Duration,
            LastPosition = TimeSpan.FromMinutes(5),
            LastViewed = DateTime.UtcNow
        };
        await db.SaveOrUpdateAsync(updatedEntry);

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Updated", all[0].Title);
        Assert.Equal(TimeSpan.FromMinutes(5), all[0].LastPosition);
    }

    [Fact]
    public async Task HistoryDatabase_ClearAsync_DeletesAll()
    {
        var db = new HistoryDatabase(MakeConnectionString());

        for (int i = 0; i < 5; i++)
        {
            await db.SaveOrUpdateAsync(new HistoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = $"v{i}",
                Title = $"Video {i}",
                Author = "Author",
                Duration = TimeSpan.FromMinutes(1),
                LastPosition = TimeSpan.Zero,
                LastViewed = DateTime.UtcNow
            });
        }

        Assert.Equal(5, (await db.GetAllAsync()).Count);

        await db.ClearAsync();
        Assert.Empty(await db.GetAllAsync());
    }

    [Fact]
    public async Task FavoritesDatabase_CRUD()
    {
        var db = new FavoritesDatabase(MakeConnectionString());

        var fav = new FavoriteEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "fav1",
            Title = "Fav Video",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(3),
            AddedAt = DateTime.UtcNow
        };

        await db.AddAsync(fav);
        Assert.True(await db.IsFavoriteAsync("fav1"));
        Assert.False(await db.IsFavoriteAsync("nope"));

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Fav Video", all[0].Title);

        await db.RemoveAsync("fav1");
        Assert.False(await db.IsFavoriteAsync("fav1"));
        Assert.Empty(await db.GetAllAsync());
    }

    [Fact]
    public async Task FavoritesDatabase_AddAsync_IgnoresDuplicate()
    {
        var db = new FavoritesDatabase(MakeConnectionString());

        var fav = new FavoriteEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "dup1",
            Title = "First",
            Author = "Author",
            Duration = TimeSpan.Zero,
            AddedAt = DateTime.UtcNow
        };

        await db.AddAsync(fav);

        var fav2 = new FavoriteEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "dup1",
            Title = "Second",
            Author = "Author",
            Duration = TimeSpan.Zero,
            AddedAt = DateTime.UtcNow
        };
        await db.AddAsync(fav2);

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("First", all[0].Title);
    }

    [Fact]
    public async Task PlaylistDatabase_CRUD()
    {
        var db = new PlaylistDatabase(MakeConnectionString());

        var playlist = await db.CreateAsync("My Playlist", "A description");
        Assert.Equal("My Playlist", playlist.Title);
        Assert.Empty(playlist.Videos);

        var all = await db.GetAllAsync();
        Assert.Single(all);

        var byId = await db.GetByIdAsync(playlist.Id);
        Assert.NotNull(byId);
        Assert.Equal("My Playlist", byId!.Title);
        Assert.Equal("A description", byId.Description);

        await db.RenameAsync(playlist.Id, "Renamed");
        var renamed = await db.GetByIdAsync(playlist.Id);
        Assert.Equal("Renamed", renamed!.Title);

        var video = new Video
        {
            Id = "vid1",
            Title = "Video 1",
            ChannelName = "Channel",
            Duration = TimeSpan.FromMinutes(5),
            ThumbnailUrl = "https://thumb.url"
        };
        await db.AddVideoAsync(playlist.Id, video);
        var withVideo = await db.GetByIdAsync(playlist.Id);
        Assert.Single(withVideo!.Videos);
        Assert.Equal("vid1", withVideo.Videos[0].Id);

        await db.RemoveVideoAsync(playlist.Id, "vid1");
        var afterRemove = await db.GetByIdAsync(playlist.Id);
        Assert.Empty(afterRemove!.Videos);

        await db.DeleteAsync(playlist.Id);
        Assert.Null(await db.GetByIdAsync(playlist.Id));
    }

    [Fact]
    public async Task PlaylistDatabase_ReorderVideoAsync()
    {
        var db = new PlaylistDatabase(MakeConnectionString());
        var playlist = await db.CreateAsync("Reorder Test", null);

        var v1 = new Video { Id = "v1", Title = "A", ChannelName = "C", Duration = TimeSpan.Zero };
        var v2 = new Video { Id = "v2", Title = "B", ChannelName = "C", Duration = TimeSpan.Zero };
        var v3 = new Video { Id = "v3", Title = "C", ChannelName = "C", Duration = TimeSpan.Zero };

        await db.AddVideoAsync(playlist.Id, v1);
        await db.AddVideoAsync(playlist.Id, v2);
        await db.AddVideoAsync(playlist.Id, v3);

        await db.ReorderVideoAsync(playlist.Id, "v3", 0);

        var reordered = await db.GetByIdAsync(playlist.Id);
        Assert.Equal(3, reordered!.Videos.Count);
        Assert.Equal("v3", reordered.Videos[0].Id);
        Assert.Equal("v1", reordered.Videos[1].Id);
        Assert.Equal("v2", reordered.Videos[2].Id);
    }

    [Fact]
    public async Task DownloadDatabase_CRUD()
    {
        var logger = Mock.Of<ILogger<DownloadDatabase>>();
        var db = new DownloadDatabase(MakeConnectionString(), logger);

        var task = new DownloadTask
        {
            VideoId = "dl1",
            Title = "Download Test",
            SourceUrl = "https://example.com/video.mp4",
            DestinationPath = "/tmp/video.mp4",
            TemporaryPath = "/tmp/video.mp4.part",
            Quality = "720",
            Format = "mp4",
            Status = DownloadStatus.Queued,
            TotalBytes = 1024 * 1024,
            CreatedAt = DateTime.UtcNow
        };

        await db.SaveAsync(task);

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("dl1", all[0].VideoId);
        Assert.Equal(DownloadStatus.Queued, all[0].Status);

        var byId = await db.GetByIdAsync(task.Id);
        Assert.NotNull(byId);

        await db.UpdateStatusAsync(task.Id, DownloadStatus.Downloading);
        var updated = await db.GetByIdAsync(task.Id);
        Assert.Equal(DownloadStatus.Downloading, updated!.Status);

        await db.UpdateProgressAsync(task.Id, 512 * 1024, 50.0);
        var progress = await db.GetByIdAsync(task.Id);
        Assert.Equal(512 * 1024, progress!.DownloadedBytes);

        await db.DeleteAsync(task.Id);
        Assert.Null(await db.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task SchemaMigration_IsIdempotent()
    {
        var cs = MakeConnectionString();
        var db = new HistoryDatabase(cs);

        var entry = new HistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            VideoId = "idem1",
            Title = "Idempotent",
            Author = "Author",
            Duration = TimeSpan.Zero,
            LastPosition = TimeSpan.Zero,
            LastViewed = DateTime.UtcNow
        };
        await db.SaveOrUpdateAsync(entry);

        var db2 = new HistoryDatabase(cs);
        var all = await db2.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("idem1", all[0].VideoId);
    }
}
