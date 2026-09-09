using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class SearchService
{
    private readonly IVideoProvider _videoProvider;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IVideoProvider videoProvider, ILogger<SearchService> logger)
    {
        _videoProvider = videoProvider;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(
        string query,
        SearchFilter filter = SearchFilter.All,
        SearchSortOrder sortOrder = SearchSortOrder.Relevance,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResult();

        try
        {
            _logger.LogInformation("Searching for '{Query}' with filter {Filter}", query, filter);

            var videos = await _videoProvider.SearchAsync(query, filter, sortOrder, cancellationToken);

            return new SearchResult
            {
                Videos = videos,
                HasMore = videos.Count >= 20
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query '{Query}'", query);
            throw;
        }
    }

    public async Task<IReadOnlyList<Video>> GetRecommendationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _videoProvider.GetRecommendationsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations");
            return Array.Empty<Video>();
        }
    }

    public async Task<IReadOnlyList<Video>> GetPopularAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _videoProvider.GetPopularAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular videos");
            return Array.Empty<Video>();
        }
    }
}
