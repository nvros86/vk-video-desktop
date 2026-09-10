using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class SettingsServiceTests
{
    [Fact]
    public async Task LoadAsync_WithNoFile_CreatesDefaultSettings()
    {
        var service = new SettingsService();
        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal("ru", service.Settings.Language);
        Assert.False(string.IsNullOrEmpty(service.Settings.DownloadFolder));
    }

    [Fact]
    public async Task SaveAsync_CreatesSettingsFile()
    {
        var service = new SettingsService();
        service.Settings.Theme = AppTheme.Light;
        service.Settings.MaxConcurrentDownloads = 3;

        await service.SaveAsync();

        var service2 = new SettingsService();
        await service2.LoadAsync();

        Assert.Equal(AppTheme.Light, service2.Settings.Theme);
        Assert.Equal(3, service2.Settings.MaxConcurrentDownloads);
    }

    [Fact]
    public async Task ResetAsync_RestoresDefaults()
    {
        var service = new SettingsService();
        service.Settings.Theme = AppTheme.Light;
        service.Settings.MaxConcurrentDownloads = 5;

        await service.ResetAsync();

        Assert.Equal(AppTheme.Dark, service.Settings.Theme);
        Assert.Equal(2, service.Settings.MaxConcurrentDownloads);
    }
}

public class HistoryServiceTests
{
    [Fact]
    public async Task SaveOrUpdateAsync_AddsNewEntry()
    {
        var service = new HistoryService();
        var entry = new HistoryEntry
        {
            VideoId = "123",
            Title = "Test",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(5),
            LastPosition = TimeSpan.FromSeconds(30),
            LastViewed = DateTime.UtcNow
        };

        await service.SaveOrUpdateAsync(entry);

        var entries = await service.GetAllAsync();
        Assert.Single(entries);
        Assert.Equal("123", entries[0].VideoId);
    }

    [Fact]
    public async Task SaveOrUpdateAsync_UpdatesExistingEntry()
    {
        var service = new HistoryService();
        var entry1 = new HistoryEntry
        {
            VideoId = "123",
            Title = "Test",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(5),
            LastPosition = TimeSpan.FromSeconds(30),
            LastViewed = DateTime.UtcNow
        };
        await service.SaveOrUpdateAsync(entry1);

        var entry2 = new HistoryEntry
        {
            VideoId = "123",
            Title = "Test Updated",
            Author = "Author",
            Duration = TimeSpan.FromMinutes(5),
            LastPosition = TimeSpan.FromSeconds(60),
            LastViewed = DateTime.UtcNow
        };
        await service.SaveOrUpdateAsync(entry2);

        var entries = await service.GetAllAsync();
        Assert.Single(entries);
        Assert.Equal(TimeSpan.FromSeconds(60), entries[0].LastPosition);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var service = new HistoryService();
        var entry = new HistoryEntry
        {
            Id = "entry1",
            VideoId = "123",
            Title = "Test",
            Author = "Author"
        };
        await service.SaveOrUpdateAsync(entry);

        await service.DeleteAsync("entry1");

        var entries = await service.GetAllAsync();
        Assert.Empty(entries);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        var service = new HistoryService();
        await service.SaveOrUpdateAsync(new HistoryEntry
        {
            VideoId = "1",
            Title = "Test1",
            Author = "Author"
        });
        await service.SaveOrUpdateAsync(new HistoryEntry
        {
            VideoId = "2",
            Title = "Test2",
            Author = "Author"
        });

        await service.ClearAsync();

        var entries = await service.GetAllAsync();
        Assert.Empty(entries);
    }

    [Fact]
    public async Task SaveOrUpdateAsync_EmptyVideoId_DoesNotThrow()
    {
        var service = new HistoryService();
        await service.SaveOrUpdateAsync(new HistoryEntry
        {
            VideoId = "",
            Title = "Test",
            Author = "Author"
        });
        var history = await service.GetAllAsync();
        Assert.Single(history);
    }

    [Fact]
    public async Task SaveOrUpdateAsync_DuplicateVideo_UpdatesTimestamp()
    {
        var service = new HistoryService();
        var entry = new HistoryEntry
        {
            VideoId = "v1",
            Title = "Test",
            Author = "Author",
            LastViewed = DateTime.UtcNow
        };
        await service.SaveOrUpdateAsync(entry);
        await service.SaveOrUpdateAsync(entry);
        var history = await service.GetAllAsync();
        Assert.Single(history);
    }
}

public class FavoritesServiceTests
{
    [Fact]
    public async Task AddAsync_AddsFavorite()
    {
        var service = new FavoritesService();
        var entry = new FavoriteEntry
        {
            VideoId = "123",
            Title = "Test",
            Author = "Author"
        };

        await service.AddAsync(entry);

        var favorites = await service.GetAllAsync();
        Assert.Single(favorites);
        Assert.True(await service.IsFavoriteAsync("123"));
    }

    [Fact]
    public async Task RemoveAsync_RemovesFavorite()
    {
        var service = new FavoritesService();
        await service.AddAsync(new FavoriteEntry
        {
            VideoId = "123",
            Title = "Test",
            Author = "Author"
        });

        await service.RemoveAsync("123");

        Assert.False(await service.IsFavoriteAsync("123"));
    }

    [Fact]
    public async Task IsFavoriteAsync_ReturnsCorrectly()
    {
        var service = new FavoritesService();

        Assert.False(await service.IsFavoriteAsync("123"));

        await service.AddAsync(new FavoriteEntry
        {
            VideoId = "123",
            Title = "Test",
            Author = "Author"
        });

        Assert.True(await service.IsFavoriteAsync("123"));
    }

    [Fact]
    public async Task AddAsync_EmptyVideoId_DoesNotThrow()
    {
        var service = new FavoritesService();
        await service.AddAsync(new FavoriteEntry
        {
            VideoId = "",
            Title = "Test",
            Author = "Author"
        });
        var favs = await service.GetAllAsync();
        Assert.Single(favs);
    }

    [Fact]
    public async Task AddAsync_DuplicateVideo_DoesNotThrow()
    {
        var service = new FavoritesService();
        var entry = new FavoriteEntry
        {
            VideoId = "v1",
            Title = "Test",
            Author = "Author"
        };
        await service.AddAsync(entry);
        await service.AddAsync(entry);
        var favs = await service.GetAllAsync();
        Assert.Single(favs);
    }

    [Fact]
    public async Task RemoveAsync_NonexistentId_DoesNotThrow()
    {
        var service = new FavoritesService();
        await service.RemoveAsync("nonexistent");
        var favs = await service.GetAllAsync();
        Assert.Empty(favs);
    }
}

public class PlaylistServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesPlaylist()
    {
        var service = new PlaylistService();
        var playlist = await service.CreateAsync("My Playlist", "Description");

        Assert.NotNull(playlist);
        Assert.Equal("My Playlist", playlist.Title);
        Assert.Equal("Description", playlist.Description);
    }

    [Fact]
    public async Task DeleteAsync_RemovesPlaylist()
    {
        var service = new PlaylistService();
        var playlist = await service.CreateAsync("My Playlist", null);

        await service.DeleteAsync(playlist.Id);

        var playlists = await service.GetAllAsync();
        Assert.Empty(playlists);
    }

    [Fact]
    public async Task AddVideoAsync_AddsVideoToPlaylist()
    {
        var service = new PlaylistService();
        var playlist = await service.CreateAsync("My Playlist", null);
        var video = new Video
        {
            Id = "123",
            Title = "Test Video"
        };

        await service.AddVideoAsync(playlist.Id, video);

        var updated = await service.GetByIdAsync(playlist.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.Videos);
    }
}
