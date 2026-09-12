using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Metrics;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.VkApi;

public sealed class VkVideoProvider : IVideoProvider
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<VkVideoProvider> _logger;
    private readonly AppMetrics _metrics;
    private const string BaseUrl = "https://api.vk.com";

    public VkVideoProvider(HttpClient httpClient, ISettingsService settingsService, ILogger<VkVideoProvider> logger, AppMetrics metrics)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
        _metrics = metrics;
    }

    private string BuildUrl(string method, Dictionary<string, string> parameters)
    {
        var token = _settingsService.Settings.AccessToken;
        if (!string.IsNullOrEmpty(token))
            parameters["access_token"] = token;
        parameters["v"] = "5.199";
        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{BaseUrl}/method/{method}?{queryString}";
    }

    private async Task<T?> GetApiAsync<T>(string url, string methodName, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<VkResponse<T>>(url, cancellationToken);
        if (response == null)
        {
            _logger.LogWarning("VK API {Method}: null response", methodName);
            return default;
        }
        if (response.Error != null)
        {
            _logger.LogWarning("VK API {Method} error {Code}: {Message}", methodName, response.Error.ErrorCode, response.Error.ErrorMsg);
            return default;
        }
        return response.Response;
    }

    public async Task<IReadOnlyList<Video>> SearchAsync(
        string query,
        SearchFilter filter,
        SearchSortOrder sortOrder,
        CancellationToken cancellationToken,
        int offset = 0)
    {
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            _logger.LogDebug("Searching VK for '{Query}' offset {Offset}", query, offset);

            var url = BuildUrl("video.search", new Dictionary<string, string>
            {
                ["q"] = query,
                ["count"] = "20",
                ["offset"] = offset.ToString(),
                ["sort"] = sortOrder switch
                {
                    SearchSortOrder.Date => "2",
                    SearchSortOrder.Popularity => "0",
                    _ => "1"
                }
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);

            if (response?.Error != null)
            {
                _logger.LogWarning("VK API video.search error {Code}: {Message}", response.Error.ErrorCode, response.Error.ErrorMsg);
                return Array.Empty<Video>();
            }

            if (response?.Response?.Items == null)
                return Array.Empty<Video>();

            return response.Response.Items.Select(MapVideo).ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            success = false;
            _logger.LogError(ex, "Auth error in search for '{Query}'", query);
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            success = false;
            _logger.LogWarning(ex, "Rate limited in search for '{Query}'", query);
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex)
        {
            success = false;
            _logger.LogError(ex, "HTTP error in search for '{Query}'", query);
            return Array.Empty<Video>();
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to search VK videos for '{Query}'", query);
            return Array.Empty<Video>();
        }
        finally
        {
            _metrics.TrackApiCall("video.search", sw.Elapsed, success);
        }
    }

    public async Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            var url = BuildUrl("video.get", new Dictionary<string, string>
            {
                ["videos"] = videoId
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoListResult>>(url, cancellationToken);

            if (response?.Error != null)
            {
                _logger.LogWarning("VK API video.get error {Code}: {Message}", response.Error.ErrorCode, response.Error.ErrorMsg);
                return null;
            }

            return response?.Response?.Items?.FirstOrDefault()?.MapToVideo();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            success = false;
            _logger.LogError(ex, "Auth error getting video {VideoId}", videoId);
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            success = false;
            _logger.LogWarning(ex, "Rate limited getting video {VideoId}", videoId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            success = false;
            _logger.LogError(ex, "HTTP error getting video {VideoId}", videoId);
            return null;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to get video {VideoId}", videoId);
            return null;
        }
        finally
        {
            _metrics.TrackApiCall("video.get", sw.Elapsed, success);
        }
    }

    public async Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            var url = BuildUrl("users.get", new Dictionary<string, string>
            {
                ["user_id"] = channelId,
                ["fields"] = "photo_200,photo_400_orig,description,subscriptions_count"
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkUserListResult>>(url, cancellationToken);

            if (response?.Error != null)
            {
                _logger.LogWarning("VK API users.get error {Code}: {Message}", response.Error.ErrorCode, response.Error.ErrorMsg);
                return null;
            }

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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            success = false;
            _logger.LogError(ex, "Auth error getting channel {ChannelId}", channelId);
            return null;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            success = false;
            _logger.LogWarning(ex, "Rate limited getting channel {ChannelId}", channelId);
            return null;
        }
        catch (HttpRequestException ex)
        {
            success = false;
            _logger.LogError(ex, "HTTP error getting channel {ChannelId}", channelId);
            return null;
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to get channel {ChannelId}", channelId);
            return null;
        }
        finally
        {
            _metrics.TrackApiCall("users.get", sw.Elapsed, success);
        }
    }

    public async Task<IReadOnlyList<Video>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        return await GetPopularAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Video>> GetPopularAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            var hasToken = !string.IsNullOrEmpty(_settingsService.Settings.AccessToken);
            _logger.LogInformation("VK API: popular request (token={Token})", hasToken);

            IReadOnlyList<Video> result;

            if (hasToken)
            {
                _logger.LogInformation("VK API video.get: fetching user videos");
                var url = BuildUrl("video.get", new Dictionary<string, string>
                {
                    ["count"] = "20"
                });
                var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);
                if (response?.Error != null)
                {
                    _logger.LogWarning("VK API video.get (user) error {Code}: {Message}", response.Error.ErrorCode, response.Error.ErrorMsg);
                    result = Array.Empty<Video>();
                }
                else
                {
                    result = response?.Response?.Items?.Select(MapVideo)?.ToList() ?? new List<Video>();
                }
            }
            else
            {
                var allVideos = new List<Video>();
                var communityIds = new[] { "-22441471", "-1", "-163004656" };

                foreach (var ownerId in communityIds)
                {
                    try
                    {
                        _logger.LogInformation("VK API video.get: fetching from community {OwnerId}", ownerId);
                        var url = BuildUrl("video.get", new Dictionary<string, string>
                        {
                            ["owner_id"] = ownerId,
                            ["count"] = "10"
                        });
                        var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);
                        if (response?.Error != null)
                        {
                            _logger.LogWarning("VK API video.get community {OwnerId} error {Code}: {Message}", ownerId, response.Error.ErrorCode, response.Error.ErrorMsg);
                            continue;
                        }
                        var videos = response?.Response?.Items?.Select(MapVideo)?.ToList();
                        if (videos != null && videos.Count > 0)
                        {
                            allVideos.AddRange(videos);
                            _logger.LogInformation("VK API video.get: community {OwnerId} returned {Count} videos", ownerId, videos.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "VK API video.get: community {OwnerId} failed", ownerId);
                    }

                    if (allVideos.Count >= 20) break;
                }

                result = allVideos.Take(20).ToList();
            }

            _logger.LogInformation("VK API: popular returned {Count} videos", result.Count);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            success = false;
            _logger.LogError(ex, "Auth error getting popular videos");
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            success = false;
            _logger.LogWarning(ex, "Rate limited getting popular videos");
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex)
        {
            success = false;
            _logger.LogError(ex, "HTTP error getting popular videos: {Status}", ex.StatusCode);
            return Array.Empty<Video>();
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to get popular videos");
            return Array.Empty<Video>();
        }
        finally
        {
            _metrics.TrackApiCall("video.get", sw.Elapsed, success);
        }
    }

    public async Task<IReadOnlyList<Video>> GetVideosByChannelAsync(
        string channelId,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var success = true;
        try
        {
            var url = BuildUrl("video.get", new Dictionary<string, string>
            {
                ["owner_id"] = channelId,
                ["count"] = "20"
            });
            var response = await _httpClient.GetFromJsonAsync<VkResponse<VkVideoSearchResult>>(url, cancellationToken);

            if (response?.Error != null)
            {
                _logger.LogWarning("VK API video.get channel {ChannelId} error {Code}: {Message}", channelId, response.Error.ErrorCode, response.Error.ErrorMsg);
                return Array.Empty<Video>();
            }

            IReadOnlyList<Video> result2 = response?.Response?.Items?.Select(MapVideo)?.ToList() ?? new List<Video>();
            return result2;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            success = false;
            _logger.LogError(ex, "Auth error getting videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            success = false;
            _logger.LogWarning(ex, "Rate limited getting videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
        catch (HttpRequestException ex)
        {
            success = false;
            _logger.LogError(ex, "HTTP error getting videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
        catch (Exception ex)
        {
            success = false;
            _logger.LogError(ex, "Failed to get videos for channel {ChannelId}", channelId);
            return Array.Empty<Video>();
        }
        finally
        {
            _metrics.TrackApiCall("video.get", sw.Elapsed, success);
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
        [JsonPropertyName("error_code")]
        public int ErrorCode { get; set; }
        [JsonPropertyName("error_msg")]
        public string? ErrorMsg { get; set; }
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
