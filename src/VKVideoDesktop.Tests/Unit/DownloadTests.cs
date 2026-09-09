using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Infrastructure.Download;

namespace VKVideoDesktop.Tests.Unit;

public class DownloadEngineTests
{
    private readonly Mock<ILogger<DownloadEngine>> _loggerMock;

    public DownloadEngineTests()
    {
        _loggerMock = new Mock<ILogger<DownloadEngine>>();
    }

    [Fact]
    public void ValidateUrl_InvalidUrl_ThrowsArgumentException()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await engine.DownloadAsync(
                "not-a-url",
                "test.mp4",
                "test.mp4.part",
                null,
                null,
                CancellationToken.None));
    }

    [Fact]
    public void ValidateUrl_FileUrl_ThrowsArgumentException()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await engine.DownloadAsync(
                "file:///C:/test.mp4",
                "test.mp4",
                "test.mp4.part",
                null,
                null,
                CancellationToken.None));
    }

    [Theory]
    [InlineData("ftp://example.com/file.mp4")]
    [InlineData("javascript:alert(1)")]
    public void ValidateUrl_UnsupportedScheme_ThrowsArgumentException(string url)
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        Assert.ThrowsAsync<ArgumentException>(async () =>
            await engine.DownloadAsync(
                url,
                "test.mp4",
                "test.mp4.part",
                null,
                null,
                CancellationToken.None));
    }
}

public class DownloadManagerTests
{
    private readonly Mock<IDownloadEngine> _engineMock;
    private readonly Mock<IDownloadSourceResolver> _resolverMock;
    private readonly Mock<IDownloadRepository> _repositoryMock;
    private readonly Mock<ILogger<DownloadManager>> _loggerMock;

    public DownloadManagerTests()
    {
        _engineMock = new Mock<IDownloadEngine>();
        _resolverMock = new Mock<IDownloadSourceResolver>();
        _repositoryMock = new Mock<IDownloadRepository>();
        _loggerMock = new Mock<ILogger<DownloadManager>>();
    }

    [Fact]
    public async Task AddAsync_CreatesDownloadTask()
    {
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<DownloadTask>()))
            .Returns(Task.CompletedTask);

        var manager = new DownloadManager(
            _engineMock.Object,
            _resolverMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object);

        var request = new DownloadRequest
        {
            VideoId = "123",
            Title = "Test Video",
            Option = new DownloadOption
            {
                Quality = "720p",
                Format = "mp4",
                SourceUrl = "https://example.com/video.mp4"
            },
            DestinationPath = @"C:\Videos\test.mp4"
        };

        var task = await manager.AddAsync(request, CancellationToken.None);

        Assert.NotNull(task);
        Assert.Equal("123", task.VideoId);
        Assert.Equal("Test Video", task.Title);
        Assert.Contains(manager.Downloads, d => d.Id == task.Id);
    }

    [Fact]
    public async Task Downloads_ReturnsAllDownloads()
    {
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<DownloadTask>()))
            .Returns(Task.CompletedTask);

        var manager = new DownloadManager(
            _engineMock.Object,
            _resolverMock.Object,
            _repositoryMock.Object,
            _loggerMock.Object);

        var request = new DownloadRequest
        {
            VideoId = "123",
            Title = "Test",
            Option = new DownloadOption { Quality = "720p", Format = "mp4" },
            DestinationPath = @"C:\Videos\test.mp4"
        };

        await manager.AddAsync(request, CancellationToken.None);

        Assert.Single(manager.Downloads);
    }
}
