using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure;

public sealed class VKWebViewService : IVKWebViewService
{
    public string GetVideoUrl(Video video)
    {
        return $"https://vk.com/video{video.Id}";
    }

    public string GetChannelUrl(string channelId)
    {
        return $"https://vk.com/{channelId}";
    }

    public string GetPlaylistUrl(string ownerId, string playlistId)
    {
        return $"https://vk.com/video{ownerId}_{playlistId}";
    }

    public bool CanHandleInWebView(Video video)
    {
        return video != null && !string.IsNullOrEmpty(video.Id);
    }
}
