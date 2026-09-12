using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Metrics;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Infrastructure.Download;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class ErrorHandlerServiceTests
{
    private readonly ErrorHandlerService _errorHandler;

    public ErrorHandlerServiceTests()
    {
        var logger = Mock.Of<ILogger<ErrorHandlerService>>();
        var localization = new LocalizationService();
        _errorHandler = new ErrorHandlerService(logger, localization);
    }

    [Theory]
    [InlineData(ErrorType.NetworkError)]
    [InlineData(ErrorType.AuthenticationError)]
    [InlineData(ErrorType.AccessDenied)]
    [InlineData(ErrorType.VideoUnavailable)]
    [InlineData(ErrorType.DownloadUnavailable)]
    [InlineData(ErrorType.RateLimited)]
    [InlineData(ErrorType.ServerError)]
    [InlineData(ErrorType.StorageError)]
    [InlineData(ErrorType.InsufficientSpace)]
    [InlineData(ErrorType.Timeout)]
    [InlineData(ErrorType.Cancelled)]
    [InlineData(ErrorType.UnknownError)]
    public void GetUserFriendlyMessage_AllErrorTypes_ReturnsNonEmpty(ErrorType errorType)
    {
        var message = _errorHandler.GetUserFriendlyMessage(errorType);
        Assert.False(string.IsNullOrEmpty(message));
    }

    [Fact]
    public void GetUserFriendlyMessage_NetworkError_ReturnsNetworkMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.NetworkError);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_AuthenticationError_ReturnsAuthMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.AuthenticationError);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_RateLimited_ReturnsRateLimitMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.RateLimited);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Timeout_ReturnsTimeoutMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.Timeout);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_Cancelled_ReturnsCancelledMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.Cancelled);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void GetUserFriendlyMessage_UnknownError_ReturnsGenericMessage()
    {
        var message = _errorHandler.GetUserFriendlyMessage(ErrorType.UnknownError);
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Theory]
    [InlineData(ErrorType.NetworkError)]
    [InlineData(ErrorType.AuthenticationError)]
    [InlineData(ErrorType.AccessDenied)]
    [InlineData(ErrorType.VideoUnavailable)]
    [InlineData(ErrorType.DownloadUnavailable)]
    [InlineData(ErrorType.RateLimited)]
    [InlineData(ErrorType.ServerError)]
    [InlineData(ErrorType.StorageError)]
    [InlineData(ErrorType.InsufficientSpace)]
    [InlineData(ErrorType.Timeout)]
    [InlineData(ErrorType.Cancelled)]
    [InlineData(ErrorType.UnknownError)]
    public void LogError_AllErrorTypes_DoesNotThrow(ErrorType errorType)
    {
        var logger = new Mock<ILogger<ErrorHandlerService>>();
        var localization = new LocalizationService();
        var handler = new ErrorHandlerService(logger.Object, localization);

        var ex = Record.Exception(() => handler.LogError(errorType));
        Assert.Null(ex);
    }

    [Fact]
    public void LogError_WithException_LogsException()
    {
        var logger = new Mock<ILogger<ErrorHandlerService>>();
        var localization = new LocalizationService();
        var handler = new ErrorHandlerService(logger.Object, localization);

        var exception = new InvalidOperationException("test error");
        handler.LogError(ErrorType.NetworkError, "test details", exception);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

public class DownloadManagerErrorTests : IDisposable
{
    private readonly Mock<IDownloadEngine> _engineMock;
    private readonly Mock<IVideoDownloadProvider> _downloadProviderMock;
    private readonly Mock<IDownloadRepository> _repositoryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<DownloadManager>> _loggerMock;
    private readonly AppMetrics _metrics;

    public DownloadManagerErrorTests()
    {
        _engineMock = new Mock<IDownloadEngine>();
        _downloadProviderMock = new Mock<IVideoDownloadProvider>();
        _repositoryMock = new Mock<IDownloadRepository>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggerMock = new Mock<ILogger<DownloadManager>>();
        _metrics = new AppMetrics();

        _settingsServiceMock.Setup(s => s.Settings).Returns(new UserSettings
        {
            MaxConcurrentDownloads = 5,
            RetryCount = 0,
            DeletePartOnCancel = true,
            SpeedLimit = 0
        });

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<DownloadTask>());
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<DownloadTask>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<DownloadStatus>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.UpdateProgressAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<double>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    public void Dispose() { }

    private DownloadManager CreateManager(int retryCount = 0)
    {
        _settingsServiceMock.Setup(s => s.Settings).Returns(new UserSettings
        {
            MaxConcurrentDownloads = 5,
            RetryCount = retryCount,
            DeletePartOnCancel = true,
            SpeedLimit = 0
        });

        return new DownloadManager(
            _engineMock.Object,
            _downloadProviderMock.Object,
            _repositoryMock.Object,
            _settingsServiceMock.Object,
            _loggerMock.Object,
            _metrics);
    }

    private DownloadRequest CreateRequest()
    {
        return new DownloadRequest
        {
            VideoId = "err_123",
            Title = "Error Test Video",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Option = new DownloadOption
            {
                Quality = "720p",
                Format = "mp4",
                Size = 1024,
                SourceUrl = "https://example.com/video.mp4"
            },
            DestinationPath = Path.Combine(Path.GetTempPath(), $"error_test_{Guid.NewGuid():N}.mp4")
        };
    }

    private void SetupEngineFailure(ErrorType errorType, string errorMessage = "Download failed")
    {
        _engineMock.Setup(e => e.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType
            });
    }

    [Fact]
    public async Task DownloadManager_EngineReturnsNetworkError_SetsFailedStatus()
    {
        SetupEngineFailure(ErrorType.NetworkError, "Network error");

        var manager = CreateManager(retryCount: 0);
        var tcsFailed = new TaskCompletionSource<DownloadTask>();
        manager.DownloadFailed += (_, task) => tcsFailed.TrySetResult(task);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(tcsFailed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Equal(tcsFailed.Task, result);

        var failedTask = await tcsFailed.Task;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
        Assert.Equal("Network error", failedTask.ErrorMessage);
    }

    [Fact]
    public async Task DownloadManager_EngineReturnsCancelled_SetsCancelledStatus()
    {
        SetupEngineFailure(ErrorType.Cancelled, "Cancelled");

        var manager = CreateManager(retryCount: 0);
        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        await Task.Delay(1000);

        Assert.Equal(DownloadStatus.Cancelled, task.Status);
    }

    [Fact]
    public async Task DownloadManager_EngineReturnsRateLimited_SetsFailedStatus()
    {
        SetupEngineFailure(ErrorType.RateLimited, "Rate limited");

        var manager = CreateManager(retryCount: 0);
        var tcsFailed = new TaskCompletionSource<DownloadTask>();
        manager.DownloadFailed += (_, task) => tcsFailed.TrySetResult(task);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(tcsFailed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Equal(tcsFailed.Task, result);

        var failedTask = await tcsFailed.Task;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
    }

    [Fact]
    public async Task DownloadManager_EngineTimesOut_SetsFailedStatus()
    {
        SetupEngineFailure(ErrorType.Timeout, "Timeout");

        var manager = CreateManager(retryCount: 0);
        var tcsFailed = new TaskCompletionSource<DownloadTask>();
        manager.DownloadFailed += (_, task) => tcsFailed.TrySetResult(task);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(tcsFailed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Equal(tcsFailed.Task, result);

        var failedTask = await tcsFailed.Task;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
    }

    [Fact]
    public async Task DownloadManager_RetryCountExhausted_SetsFailedStatus()
    {
        var callCount = 0;
        _engineMock.Setup(e => e.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref callCount);
                return Task.FromResult(new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Persistent error",
                    ErrorType = ErrorType.NetworkError
                });
            });

        var manager = CreateManager(retryCount: 2);
        var tcsFailed = new TaskCompletionSource<DownloadTask>();
        manager.DownloadFailed += (_, task) => tcsFailed.TrySetResult(task);

        await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(tcsFailed.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Equal(tcsFailed.Task, result);

        var failedTask = await tcsFailed.Task;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
        Assert.Equal(3, callCount);
    }
}
