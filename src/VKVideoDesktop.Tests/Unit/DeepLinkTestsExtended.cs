using VKVideoDesktop.Application.Services;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class DeepLinkTestsExtended
{
    private readonly DeepLinkService _service = new();

    [Fact]
    public void ProcessString_VideoUrl_WithTimestamp_ParsesVideoId()
    {
        var result = _service.ProcessString("vkvideo://video/-123_456");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("-123_456", result.VideoId);
    }

    [Fact]
    public void ProcessString_VideoUrl_MobileFormat_ReturnsNull()
    {
        var result = _service.ProcessString("https://m.vk.com/video-123_456");
        Assert.Null(result);
    }

    [Fact]
    public void ProcessString_ChannelUrl_UsernameFormat_ParsesChannelId()
    {
        var result = _service.ProcessString("vkvideo://channel/@username");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Channel, result.Type);
        Assert.Equal("@username", result.ChannelId);
    }

    [Fact]
    public void ProcessString_ChannelUrl_NumericFormat_ParsesChannelId()
    {
        var result = _service.ProcessString("vkvideo://channel/club123");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Channel, result.Type);
        Assert.Equal("club123", result.ChannelId);
    }

    [Fact]
    public void ProcessString_SearchUrl_EncodedQuery_ParsesQuery()
    {
        var result = _service.ProcessString("vkvideo://search/hello%20world");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Search, result.Type);
        Assert.Equal("hello world", result.Query);
    }

    [Fact]
    public void ProcessString_SearchUrl_CyrillicQuery_ParsesQuery()
    {
        var result = _service.ProcessString("vkvideo://search/%D1%82%D0%B5%D1%81%D1%82");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Search, result.Type);
        Assert.Equal("тест", result.Query);
    }

    [Fact]
    public void ProcessString_PlaylistUrl_ParsesPlaylistId()
    {
        var result = _service.ProcessString("vkvideo://playlist/123_456/789");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Playlist, result.Type);
        Assert.Equal("123_456", result.OwnerId);
        Assert.Equal("789", result.PlaylistId);
    }

    [Fact]
    public void ProcessString_VideoUrl_InvalidFormat_ReturnsNull()
    {
        var result = _service.ProcessString("vkvideo://invalid");
        Assert.Null(result);
    }

    [Fact]
    public void ProcessString_VideoUrl_EmptyOwnerId_ReturnsNull()
    {
        var result = _service.ProcessString("vkvideo://video_123");
        Assert.Null(result);
    }

    [Fact]
    public void ProcessString_HttpUrl_VkDomain_ReturnsNull()
    {
        var result = _service.ProcessString("http://vkvideo.ru/video-123_456");
        Assert.Null(result);
    }

    [Fact]
    public void ProcessString_VideoUrl_WithMultipleSegments_ParsesFirst()
    {
        var result = _service.ProcessString("vkvideo://video/123_456/extra");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("123_456", result.VideoId);
    }

    [Fact]
    public void ProcessString_ChannelUrl_DeepPath_ParsesChannelId()
    {
        var result = _service.ProcessString("vkvideo://channel/12345/subpath");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Channel, result.Type);
        Assert.Equal("12345", result.ChannelId);
    }

    [Fact]
    public void ProcessString_SearchUrl_PlusEncoded_ParsesQuery()
    {
        var result = _service.ProcessString("vkvideo://search/hello+world");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Search, result.Type);
        Assert.Equal("hello+world", result.Query);
    }

    [Fact]
    public void ProcessString_CaseInsensitive_UriNormalizesScheme()
    {
        var result = _service.ProcessString("VKVIDEO://video/123_456");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("123_456", result.VideoId);
    }

    [Fact]
    public void ProcessString_TrailingSlash_ParsesCorrectly()
    {
        var result = _service.ProcessString("vkvideo://video/123_456/");
        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("123_456", result.VideoId);
    }
}
