using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class FavoritesService : IFavoritesService
{
    private readonly List<FavoriteEntry> _favorites = new();
    private readonly object _lock = new();

    public Task<IReadOnlyList<FavoriteEntry>> GetAllAsync()
    {
        lock (_lock)
        {
            IReadOnlyList<FavoriteEntry> result = _favorites
                .OrderByDescending(f => f.AddedAt)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> IsFavoriteAsync(string videoId)
    {
        lock (_lock)
        {
            return Task.FromResult(_favorites.Any(f => f.VideoId == videoId));
        }
    }

    public Task AddAsync(FavoriteEntry entry)
    {
        lock (_lock)
        {
            if (!_favorites.Any(f => f.VideoId == entry.VideoId))
            {
                _favorites.Add(entry);
            }
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string videoId)
    {
        lock (_lock)
        {
            _favorites.RemoveAll(f => f.VideoId == videoId);
        }
        return Task.CompletedTask;
    }
}
