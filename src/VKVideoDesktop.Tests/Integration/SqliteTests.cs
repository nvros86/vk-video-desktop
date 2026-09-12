using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Data.Database;
using Xunit;

namespace VKVideoDesktop.Tests.Integration;

public class SqliteTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
    }

    private string CreateTempDb()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sqlite_test_{Guid.NewGuid():N}.db");
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public async Task HistoryDatabase_NullableFields_MapsCorrectly()
    {
        var db = new HistoryDatabase($"Data Source={CreateTempDb()}");

        var entry = new HistoryEntry
        {
            VideoId = "nullable_test",
            Title = "Nullable Test",
            Author = string.Empty,
            ThumbnailUrl = null!,
            Duration = TimeSpan.FromMinutes(5),
            LastPosition = TimeSpan.FromMinutes(1),
            LastViewed = DateTime.UtcNow
        };

        await db.SaveOrUpdateAsync(entry);

        var loaded = await db.GetByVideoIdAsync("nullable_test");
        Assert.NotNull(loaded);
        Assert.Equal(string.Empty, loaded!.ThumbnailUrl);
    }

    [Fact]
    public async Task HistoryDatabase_ZeroDuration_MapsCorrectly()
    {
        var db = new HistoryDatabase($"Data Source={CreateTempDb()}");

        var entry = new HistoryEntry
        {
            VideoId = "zero_dur",
            Title = "Zero Duration",
            Author = "Author",
            Duration = TimeSpan.Zero,
            LastPosition = TimeSpan.Zero,
            LastViewed = DateTime.UtcNow
        };

        await db.SaveOrUpdateAsync(entry);

        var loaded = await db.GetByVideoIdAsync("zero_dur");
        Assert.NotNull(loaded);
        Assert.Equal(TimeSpan.Zero, loaded!.Duration);
        Assert.Equal(TimeSpan.Zero, loaded.LastPosition);
    }

    [Fact]
    public async Task HistoryDatabase_LargeTicks_MapsCorrectly()
    {
        var db = new HistoryDatabase($"Data Source={CreateTempDb()}");

        var entry = new HistoryEntry
        {
            VideoId = "large_ticks",
            Title = "Large Ticks",
            Author = "Author",
            Duration = TimeSpan.MaxValue,
            LastPosition = TimeSpan.MaxValue,
            LastViewed = DateTime.UtcNow
        };

        await db.SaveOrUpdateAsync(entry);

        var loaded = await db.GetByVideoIdAsync("large_ticks");
        Assert.NotNull(loaded);
        Assert.Equal(TimeSpan.MaxValue, loaded!.Duration);
        Assert.Equal(TimeSpan.MaxValue, loaded.LastPosition);
    }

    [Fact]
    public async Task DownloadDatabase_NullableFields_MapsCorrectly()
    {
        var logger = Mock.Of<ILogger<DownloadDatabase>>();
        var db = new DownloadDatabase($"Data Source={CreateTempDb()}", logger);

        var task = new DownloadTask
        {
            VideoId = "nullable_dl",
            Title = "Nullable Download",
            ThumbnailUrl = string.Empty,
            SourceUrl = string.Empty,
            DestinationPath = "/tmp/test.mp4",
            TemporaryPath = string.Empty,
            Quality = null,
            Format = null,
            Status = DownloadStatus.Queued,
            TotalBytes = null,
            DownloadedBytes = 0,
            SpeedBytesPerSecond = 0,
            RetryCount = 0,
            ErrorMessage = null,
            CreatedAt = DateTime.UtcNow,
            StartedAt = null,
            CompletedAt = null
        };

        await db.SaveAsync(task);

        var loaded = await db.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);
        Assert.Equal(string.Empty, loaded!.ThumbnailUrl);
        Assert.Equal(string.Empty, loaded.SourceUrl);
        Assert.Equal(string.Empty, loaded.TemporaryPath);
        Assert.Null(loaded.Quality);
        Assert.Null(loaded.Format);
        Assert.Null(loaded.TotalBytes);
        Assert.Null(loaded.ErrorMessage);
        Assert.Null(loaded.StartedAt);
        Assert.Null(loaded.CompletedAt);
    }

    [Fact]
    public async Task DownloadDatabase_NullableFields_WithValues_MapsCorrectly()
    {
        var logger = Mock.Of<ILogger<DownloadDatabase>>();
        var db = new DownloadDatabase($"Data Source={CreateTempDb()}", logger);

        var now = DateTime.UtcNow;
        var task = new DownloadTask
        {
            VideoId = "filled_dl",
            Title = "Filled Download",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            SourceUrl = "https://example.com/video.mp4",
            DestinationPath = "/tmp/test.mp4",
            TemporaryPath = "/tmp/test.mp4.part",
            Quality = "720p",
            Format = "mp4",
            Status = DownloadStatus.Downloading,
            TotalBytes = 1024 * 1024,
            DownloadedBytes = 512 * 1024,
            SpeedBytesPerSecond = 1024 * 100,
            RetryCount = 1,
            ErrorMessage = "some error",
            CreatedAt = now,
            StartedAt = now,
            CompletedAt = now.AddMinutes(5)
        };

        await db.SaveAsync(task);

        var loaded = await db.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);
        Assert.Equal("https://example.com/thumb.jpg", loaded!.ThumbnailUrl);
        Assert.Equal("https://example.com/video.mp4", loaded.SourceUrl);
        Assert.Equal("/tmp/test.mp4.part", loaded.TemporaryPath);
        Assert.Equal("720p", loaded.Quality);
        Assert.Equal("mp4", loaded.Format);
        Assert.Equal(1024 * 1024, loaded.TotalBytes);
        Assert.Equal("some error", loaded.ErrorMessage);
        Assert.NotNull(loaded.StartedAt);
        Assert.NotNull(loaded.CompletedAt);
    }

    [Fact]
    public void SchemaMigration_IsIdempotent()
    {
        using var connection = new SqliteConnection($"Data Source={CreateTempDb()}");
        connection.Open();

        SchemaMigration.EnsureMigrationsTable(connection);
        SchemaMigration.Apply(connection, 1, "First migration", new[]
        {
            "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT)"
        });

        SchemaMigration.Apply(connection, 1, "First migration again", new[]
        {
            "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT)"
        });

        Assert.True(SchemaMigration.IsApplied(connection, 1));
    }

    [Fact]
    public void SchemaMigration_IsApplied_ChecksCorrectly()
    {
        using var connection = new SqliteConnection($"Data Source={CreateTempDb()}");
        connection.Open();

        SchemaMigration.EnsureMigrationsTable(connection);

        Assert.False(SchemaMigration.IsApplied(connection, 1));

        SchemaMigration.Apply(connection, 1, "Test migration", new[]
        {
            "CREATE TABLE IF NOT EXISTS TestTable (Id INTEGER PRIMARY KEY, Name TEXT)"
        });

        Assert.True(SchemaMigration.IsApplied(connection, 1));
        Assert.False(SchemaMigration.IsApplied(connection, 2));
    }

    [Fact]
    public async Task HistoryDatabase_ConcurrentWrites_DoesNotCorrupt()
    {
        var db = new HistoryDatabase($"Data Source={CreateTempDb()}");

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var entry = new HistoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = $"concurrent_{i}",
                Title = $"Concurrent Video {i}",
                Author = $"Author {i}",
                ThumbnailUrl = $"https://thumb/{i}.jpg",
                Duration = TimeSpan.FromMinutes(i),
                LastPosition = TimeSpan.FromSeconds(i * 10),
                LastViewed = DateTime.UtcNow
            };
            await db.SaveOrUpdateAsync(entry);
        }));

        await Task.WhenAll(tasks);

        var all = await db.GetAllAsync();
        Assert.Equal(10, all.Count);
    }

    [Fact]
    public async Task DownloadDatabase_ConcurrentWrites_DoesNotCorrupt()
    {
        var logger = Mock.Of<ILogger<DownloadDatabase>>();
        var db = new DownloadDatabase($"Data Source={CreateTempDb()}", logger);

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var task = new DownloadTask
            {
                VideoId = $"concurrent_dl_{i}",
                Title = $"Concurrent Download {i}",
                DestinationPath = $"/tmp/test_{i}.mp4",
                Status = DownloadStatus.Queued,
                CreatedAt = DateTime.UtcNow
            };
            await db.SaveAsync(task);
        }));

        await Task.WhenAll(tasks);

        var all = await db.GetAllAsync();
        Assert.Equal(10, all.Count);
    }

    [Fact]
    public async Task FavoritesDatabase_AddAsync_IgnoresDuplicate()
    {
        var db = new FavoritesDatabase($"Data Source={CreateTempDb()}");

        var fav1 = new FavoriteEntry
        {
            VideoId = "dup_video",
            Title = "First Add",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(5),
            AddedAt = DateTime.UtcNow
        };

        var fav2 = new FavoriteEntry
        {
            VideoId = "dup_video",
            Title = "Second Add",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(5),
            AddedAt = DateTime.UtcNow
        };

        await db.AddAsync(fav1);
        await db.AddAsync(fav2);

        var all = await db.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("First Add", all[0].Title);
        Assert.True(await db.IsFavoriteAsync("dup_video"));
    }

    [Fact]
    public async Task PlaylistDatabase_AddVideoAsync_ConcurrentAdd_DoesNotLoseVideos()
    {
        var db = new PlaylistDatabase($"Data Source={CreateTempDb()}");
        var playlist = await db.CreateAsync("Concurrent Test", null);

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var video = new Video
            {
                Id = $"video_{i}",
                Title = $"Video {i}",
                ChannelName = "Channel",
                Duration = TimeSpan.FromMinutes(i + 1)
            };
            await db.AddVideoAsync(playlist.Id, video);
        }));

        await Task.WhenAll(tasks);

        var loaded = await db.GetByIdAsync(playlist.Id);
        Assert.NotNull(loaded);
        Assert.Equal(10, loaded!.Videos.Count);
    }
}
