using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Infrastructure.Download;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class DownloadEngineTests
{
    private readonly Mock<ILogger<DownloadEngine>> _loggerMock;

    public DownloadEngineTests()
    {
        _loggerMock = new Mock<ILogger<DownloadEngine>>();
    }

    [Fact]
    public async Task ValidateUrl_InvalidUrl_ReturnsErrorResult()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var result = await engine.DownloadAsync(
            "not-a-url",
            "test.mp4",
            "test.mp4.part",
            null,
            0,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUrl_FileUrl_ReturnsErrorResult()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var result = await engine.DownloadAsync(
            "file:///C:/test.mp4",
            "test.mp4",
            "test.mp4.part",
            null,
            0,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Theory]
    [InlineData("ftp://example.com/file.mp4")]
    [InlineData("javascript:alert(1)")]
    public async Task ValidateUrl_UnsupportedScheme_ReturnsErrorResult(string url)
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var result = await engine.DownloadAsync(
            url,
            "test.mp4",
            "test.mp4.part",
            null,
            0,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUrl_Http_ReturnsErrorResult()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var result = await engine.DownloadAsync(
            "http://example.com/video.mp4",
            "test.mp4",
            "test.mp4.part",
            null,
            0,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("HTTPS", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUrl_Https_ReturnsNetworkError()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var result = await engine.DownloadAsync(
            "https://nonexistent.invalid/video.mp4",
            "test.mp4",
            "test.mp4.part",
            null,
            0,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }
}
