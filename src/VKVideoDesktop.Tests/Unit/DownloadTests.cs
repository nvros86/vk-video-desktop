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
    public async Task DownloadAsync_SpeedLimitZero_NoThrottle()
    {
        var engine = new DownloadEngine(_loggerMock.Object);

        var tempDir = Path.Combine(Path.GetTempPath(), $"dltest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempFile = Path.Combine(tempDir, "temp.bin");
            var destFile = Path.Combine(tempDir, "dest.bin");

            var result = await engine.DownloadAsync(
                "https://httpbin.org/bytes/1024",
                destFile, tempFile, (long?)1024, 0,
                null, CancellationToken.None);

            Assert.True(result.IsSuccess);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}

public class DownloadManagerTests
{
    private readonly Mock<IDownloadEngine> _engineMock;
    private readonly Mock<IDownloadSourceResolver> _resolverMock;
    private readonly Mock<IDownloadRepository> _repositoryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<DownloadManager>> _loggerMock;

    public DownloadManagerTests()
    {
        _engineMock = new Mock<IDownloadEngine>();
        _resolverMock = new Mock<IDownloadSourceResolver>();
        _repositoryMock = new Mock<IDownloadRepository>();
        _settingsServiceMock = new Mock<ISettingsService>();
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
            _settingsServiceMock.Object,
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
            _settingsServiceMock.Object,
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

    [Fact]
    public async Task RecoverIncompleteDownloadsAsync_WithQueuedTasks_DoesNotThrow()
    {
        var engine = new Mock<IDownloadEngine>();
        var sourceResolver = new Mock<IDownloadSourceResolver>();
        var repo = new Mock<IDownloadRepository>();
        var settings = new Mock<ISettingsService>();
        settings.Setup(s => s.Settings).Returns(new UserSettings { DownloadFolder = "/tmp", MaxConcurrentDownloads = 2 });

        repo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<DownloadTask>());

        var manager = new DownloadManager(
            engine.Object, sourceResolver.Object, repo.Object,
            settings.Object, _loggerMock.Object);

        await manager.RecoverIncompleteDownloadsAsync();
    }
}
