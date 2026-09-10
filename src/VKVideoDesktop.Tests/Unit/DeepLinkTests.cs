using VKVideoDesktop.Application.Services;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class DeepLinkTests
{
    private readonly DeepLinkService _service = new();

    [Fact]
    public void ProcessString_VideoLink_ReturnsVideoResult()
    {
        var result = _service.ProcessString("vkvideo://video/-123_456");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("-123_456", result.VideoId);
    }

    [Fact]
    public void ProcessString_SearchLink_ReturnsSearchResult()
    {
        var result = _service.ProcessString("vkvideo://search/test%20query");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Search, result.Type);
        Assert.Equal("test query", result.Query);
    }

    [Fact]
    public void ProcessString_ChannelLink_ReturnsChannelResult()
    {
        var result = _service.ProcessString("vkvideo://channel/12345");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Channel, result.Type);
        Assert.Equal("12345", result.ChannelId);
    }

    [Fact]
    public void ProcessString_PlaylistLink_ReturnsPlaylistResult()
    {
        var result = _service.ProcessString("vkvideo://playlist/123/456");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Playlist, result.Type);
        Assert.Equal("123", result.OwnerId);
        Assert.Equal("456", result.PlaylistId);
    }

    [Fact]
    public void ProcessString_EmptyOrNull_ReturnsNull()
    {
        Assert.Null(_service.ProcessString(""));
        Assert.Null(_service.ProcessString("  "));
        Assert.Null(_service.ProcessString(null!));
    }

    [Fact]
    public void ProcessString_UnknownPath_ReturnsNull()
    {
        Assert.Null(_service.ProcessString("vkvideo://unknown"));
    }

    [Fact]
    public void ProcessString_NonVkUrl_ReturnsNull()
    {
        Assert.Null(_service.ProcessString("https://google.com"));
    }
}
