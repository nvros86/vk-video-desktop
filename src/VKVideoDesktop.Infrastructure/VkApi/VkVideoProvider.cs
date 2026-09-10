using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.VkApi;

public sealed class VkVideoProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<VkVideoProvider> _logger;
    private const string BaseUrl = "https://api.vk.com/method";

    public VkVideoProvider(HttpClient httpClient, ISettingsService settingsService, ILogger<VkVideoProvider> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    private string BuildUrl(string method, Dictionary<string, string> parameters)
    {
        parameters["access_token"] = _settingsService.Settings.AccessToken ?? string.Empty;
        parameters["v"] = "5.199";
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{BaseUrl}/method/{method}?{queryString}";
    }

    public async Task<IReadOnlyList<Video>> SearchAsync(
        string query,
        SearchFilter filter,
        SearchSortOrder sortOrder,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Searching VK for '{Query}'", query);

            var url = BuildUrl("video.search", new Dictionary<string, string>
            {
                ["q"] = query,
                ["count"] = "20",
                ["sort"] = sortOrder switch
                {
                    SearchSortOrder.Date => "2",
                    SearchSortOrder.Popularity => "0",
                    _ => "1"
                }
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);

            if (response?.Response?.Items == null)
                return Array.Empty<Video>();

            return response.Response.Items.Select(MapVideo).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search VK videos for '{Query}'", query);
            return Array.Empty<Video>();
        }
    }

    public async Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            var url = BuildUrl("video.get", new Dictionary<string, string>
            {
                ["videos"] = videoId
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoListResult>>(url, cancellationToken);

            return response?.Response?.Items?.FirstOrDefault()?.MapToVideo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video {VideoId}", videoId);
            return null;
        }
    }

    public async Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        try
        {
            var url = BuildUrl("users.get", new Dictionary<string, string>
            {
                ["user_id"] = channelId,
                ["fields"] = "photo_200,photo_400_orig,description,subscriptions_count"
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkUserListResult>>(url, cancellationToken);

            var user = response?.Response?.Items?.FirstOrDefault();
            if (user == null) return null;

            return new Channel
            {
                Id = user.Id.ToString(),
                Name = $"{user.FirstName} {user.LastName}",
                Username = user.Domain ?? string.Empty,
                AvatarUrl = user.Photo200 ?? string.Empty,
                BannerUrl = user.Photo400Orig,
                Description = user.Description ?? string.Empty,
                SubscriberCount = user.SubscriptionsCount ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get channel {ChannelId}", channelId);
            return null;
        }
    }

    public async Task<IReadOnlyList<Video>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        return await GetPopularAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Video>> GetPopularAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = BuildUrl("video.get", new Dictionary<string, string>
            {
                ["count"] = "20"
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);

            IReadOnlyList<Video> result = response?.Response?.Items?.Select(MapVideo)?.ToList() ?? new List<Video>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popular videos");
            return Array.Empty<Video>();
        }
    }

    public async Task<IReadOnlyList<Video>> GetVideosByChannelAsync(
        string channelId,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = BuildUrl("video.get", new Dictionary<string, string>
            {
                ["owner_id"] = channelId,
                ["count"] = "20"
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);

            IReadOnlyList<Video> result2 = response?.Response?.Items?.Select(MapVideo)?.ToList() ?? new List<Video>();
            return result2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
    }

    private static string? GetBestPlaybackUrl(VkVideoItem item)
    {
        if (item.Files != null && item.Files.Count > 0)
        {
            var best = item.Files
                .Where(f => f.Key.StartsWith("mp4"))
                .OrderByDescending(f =>
                {
                    if (int.TryParse(f.Key.Replace("mp4_", ""), out var h)) return h;
                    return 0;
                })
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(best.Value))
                return best.Value;
        }
        return item.Player;
    }

    private static Video MapVideo(VkVideoItem item)
    {
        return new Video
        {
            Id = $"{item.OwnerId}_{item.Id}",
            Title = item.Title ?? string.Empty,
            Description = item.Description ?? string.Empty,
            ThumbnailUrl = item.Image?.LastOrDefault()?.Url ?? string.Empty,
            ChannelId = item.OwnerId.ToString(),
            Duration = TimeSpan.FromSeconds(item.Duration),
            ViewCount = item.Views,
            PublishedAt = DateTimeOffset.FromUnixTimeSeconds(item.Date).DateTime,
            IsLive = item.Platform == 7,
            PlaybackUrl = GetBestPlaybackUrl(item),
            QualityUrls = item.Files?.Where(f => f.Key.StartsWith("mp4") && !string.IsNullOrEmpty(f.Value))
                .ToDictionary(f => f.Key.Replace("mp4_", ""), f => f.Value)
        };
    }

    private sealed class VkResponse<T>
    {
        public T? Response { get; set; }
        public VkError? Error { get; set; }
    }

    private sealed class VkError
    {
        public int Error_Code { get; set; }
        public string? Error_Msg { get; set; }
    }

    private sealed class VkVideoSearchResult
    {
        public int Count { get; set; }
        public List<VkVideoItem>? Items { get; set; }
    }

    private sealed class VkVideoListResult
    {
        public int Count { get; set; }
        public List<VkVideoItem>? Items { get; set; }
    }

    private sealed class VkVideoItem
    {
        public long Id { get; set; }
        public long OwnerId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int Duration { get; set; }
        public long Views { get; set; }
        public long Date { get; set; }
        public int? Platform { get; set; }
        public List<VkImage>? Image { get; set; }
        public string? Player { get; set; }
        public Dictionary<string, string>? Files { get; set; }

        public Video MapToVideo()
        {
            return new Video
            {
                Id = $"{OwnerId}_{Id}",
                Title = Title ?? string.Empty,
                Description = Description ?? string.Empty,
                ThumbnailUrl = Image?.LastOrDefault()?.Url ?? string.Empty,
                ChannelId = OwnerId.ToString(),
                Duration = TimeSpan.FromSeconds(Duration),
                ViewCount = Views,
                PublishedAt = DateTimeOffset.FromUnixTimeSeconds(Date).DateTime,
                IsLive = Platform == 7,
                PlaybackUrl = GetBestPlaybackUrl(this),
                QualityUrls = Files?.Where(f => f.Key.StartsWith("mp4") && !string.IsNullOrEmpty(f.Value))
                    .ToDictionary(f => f.Key.Replace("mp4_", ""), f => f.Value)
            };
        }
    }

    private sealed class VkImage
    {
        public string? Url { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }

    private sealed class VkUserResult
    {
        public long Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Domain { get; set; }
        public string? Photo200 { get; set; }
        public string? Photo400Orig { get; set; }
        public string? Description { get; set; }
        public long? SubscriptionsCount { get; set; }
    }

    private sealed class VkUserListResult
    {
        public int Count { get; set; }
        public List<VkUserResult>? Items { get; set; }
    }
}
