using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VKVideoDesktop.Application.Services;

public sealed class DeepLinkService
{
    private static readonly Regex UrlPattern = new(@"^vkvideo(?:://)?(.+)$", RegexOptions.Compiled);

    public DeepLinkResult? ProcessUri(Uri uri)
    {
        var match = UrlPattern.Match(uri.ToString());
        if (!match.Success) return null;

        var path = match.Groups[1].Value.TrimEnd('/');
        return ParsePath(path);
    }

    public DeepLinkResult? ProcessString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return ProcessUri(uri);

        return ParsePath(input);
    }

    private DeepLinkResult? ParsePath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return null;

        return segments[0].ToLowerInvariant() switch
        {
            "video" when segments.Length >= 2 => new DeepLinkResult
            {
                Type = DeepLinkType.Video,
                VideoId = segments[1]
            },
            "playlist" when segments.Length >= 3 => new DeepLinkResult
            {
                Type = DeepLinkType.Playlist,
                OwnerId = segments[1],
                PlaylistId = segments[2]
            },
            "channel" when segments.Length >= 2 => new DeepLinkResult
            {
                Type = DeepLinkType.Channel,
                ChannelId = segments[1]
            },
            "search" when segments.Length >= 2 => new DeepLinkResult
            {
                Type = DeepLinkType.Search,
                Query = Uri.UnescapeDataString(segments[1])
            },
            _ => null
        };
    }
}

public sealed class DeepLinkResult
{
    public DeepLinkType Type { get; set; }
    public string? VideoId { get; set; }
    public string? OwnerId { get; set; }
    public string? PlaylistId { get; set; }
    public string? ChannelId { get; set; }
    public string? Query { get; set; }
}

public enum DeepLinkType
{
    Video,
    Playlist,
    Channel,
    Search
}
