using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class HistoryService : IHistoryService
{
    private readonly List<HistoryEntry> _entries = new();
    private readonly object _lock = new();

    public Task<IReadOnlyList<HistoryEntry>> GetAllAsync()
    {
        lock (_lock)
        {
            IReadOnlyList<HistoryEntry> result = _entries
                .OrderByDescending(e => e.LastViewed)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<HistoryEntry?> GetByVideoIdAsync(string videoId)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => e.VideoId == videoId);
            return Task.FromResult(entry);
        }
    }

    public Task SaveOrUpdateAsync(HistoryEntry entry)
    {
        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(e => e.VideoId == entry.VideoId);
            if (existing != null)
            {
                _entries.Remove(existing);
            }
            _entries.Add(entry);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string entryId)
    {
        lock (_lock)
        {
            _entries.RemoveAll(e => e.Id == entryId);
        }
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
        return Task.CompletedTask;
    }
}
