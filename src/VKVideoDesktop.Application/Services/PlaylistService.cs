using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class PlaylistService : IPlaylistService
{
    private readonly List<Playlist> _playlists = new();
    private readonly object _lock = new();

    public Task<IReadOnlyList<Playlist>> GetAllAsync()
    {
        lock (_lock)
        {
            IReadOnlyList<Playlist> result = _playlists.ToList();
            return Task.FromResult(result);
        }
    }

    public Task<Playlist?> GetByIdAsync(string playlistId)
    {
        lock (_lock)
        {
            var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
            return Task.FromResult(playlist);
        }
    }

    public Task<Playlist> CreateAsync(string title, string? description)
    {
        lock (_lock)
        {
            var playlist = new Playlist
            {
                Title = title,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };
            _playlists.Add(playlist);
            return Task.FromResult(playlist);
        }
    }

    public Task RenameAsync(string playlistId, string newTitle)
    {
        lock (_lock)
        {
            var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null)
            {
                var index = _playlists.IndexOf(playlist);
                _playlists[index] = playlist with { Title = newTitle };
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string playlistId)
    {
        lock (_lock)
        {
            _playlists.RemoveAll(p => p.Id == playlistId);
        }
        return Task.CompletedTask;
    }

    public Task AddVideoAsync(string playlistId, Video video)
    {
        lock (_lock)
        {
            var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null)
            {
                var index = _playlists.IndexOf(playlist);
                var updated = playlist with
                {
                    Videos = new List<Video>(playlist.Videos) { video },
                    VideoCount = playlist.VideoCount + 1
                };
                _playlists[index] = updated;
            }
        }
        return Task.CompletedTask;
    }

    public Task RemoveVideoAsync(string playlistId, string videoId)
    {
        lock (_lock)
        {
            var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null)
            {
                var index = _playlists.IndexOf(playlist);
                var videos = new List<Video>(playlist.Videos);
                videos.RemoveAll(v => v.Id == videoId);
                _playlists[index] = playlist with
                {
                    Videos = videos,
                    VideoCount = videos.Count
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task ReorderVideoAsync(string playlistId, string videoId, int newIndex)
    {
        lock (_lock)
        {
            var playlist = _playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null)
            {
                var videos = new List<Video>(playlist.Videos);
                var video = videos.FirstOrDefault(v => v.Id == videoId);
                if (video != null)
                {
                    videos.Remove(video);
                    newIndex = Math.Clamp(newIndex, 0, videos.Count);
                    videos.Insert(newIndex, video);
                    var index = _playlists.IndexOf(playlist);
                    _playlists[index] = playlist with { Videos = videos };
                }
            }
        }
        return Task.CompletedTask;
    }
}
