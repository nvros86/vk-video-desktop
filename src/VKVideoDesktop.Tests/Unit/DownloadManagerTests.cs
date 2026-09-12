using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Metrics;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Infrastructure.Download;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class DownloadManagerTests : IDisposable
{
    private readonly Mock<IDownloadEngine> _engineMock;
    private readonly Mock<IVideoDownloadProvider> _downloadProviderMock;
    private readonly Mock<IDownloadRepository> _repositoryMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILogger<DownloadManager>> _loggerMock;
    private readonly AppMetrics _metrics;
    private readonly List<string> _tempFiles = new();

    public DownloadManagerTests()
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
            RetryCount = 3,
            DeletePartOnCancel = true,
            SpeedLimit = 0
        });

        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<DownloadTask>());
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<DownloadTask>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.UpdateStatusAsync(It.IsAny<string>(), It.IsAny<DownloadStatus>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.UpdateProgressAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<double>())).Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
                if (File.Exists(file + ".part")) File.Delete(file + ".part");
            }
            catch { }
        }
    }

    private DownloadManager CreateManager(
        int maxConcurrentDownloads = 5,
        int retryCount = 3,
        bool deletePartOnCancel = true)
    {
        _settingsServiceMock.Setup(s => s.Settings).Returns(new UserSettings
        {
            MaxConcurrentDownloads = maxConcurrentDownloads,
            RetryCount = retryCount,
            DeletePartOnCancel = deletePartOnCancel,
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

    private DownloadRequest CreateRequest(
        string? sourceUrl = "https://example.com/video.mp4",
        string? destinationPath = null)
    {
        destinationPath ??= Path.Combine(Path.GetTempPath(), $"test_video_{Guid.NewGuid():N}.mp4");
        _tempFiles.Add(destinationPath);
        return new DownloadRequest
        {
            VideoId = "123_456",
            Title = "Test Video",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Option = new DownloadOption
            {
                Quality = "720p",
                Format = "mp4",
                Size = 1024,
                SourceUrl = sourceUrl
            },
            DestinationPath = destinationPath
        };
    }

    private static (Task<DownloadTask> Completed, Task<DownloadTask> Failed) SubscribeToEvents(DownloadManager manager)
    {
        var tcsCompleted = new TaskCompletionSource<DownloadTask>();
        var tcsFailed = new TaskCompletionSource<DownloadTask>();

        manager.DownloadCompleted += (_, task) => tcsCompleted.TrySetResult(task);
        manager.DownloadFailed += (_, task) => tcsFailed.TrySetResult(task);

        return (tcsCompleted.Task, tcsFailed.Task);
    }

    private void SetupEngineSuccess(string? filePath = null)
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
                IsSuccess = true,
                FilePath = filePath ?? "output.mp4"
            });
    }

    private void SetupEngineFailure(
        string errorMessage = "Download failed",
        ErrorType errorType = ErrorType.NetworkError)
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

    private sealed class EngineGate
    {
        public TaskCompletionSource<DownloadResult> Completion { get; } = new();
        public TaskCompletionSource EngineEntered { get; } = new();
    }

    private EngineGate SetupEngineBlocking()
    {
        var gate = new EngineGate();

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
                gate.EngineEntered.TrySetResult();
                return gate.Completion.Task;
            });

        return gate;
    }

    private EngineGate SetupEngineBlockingPerCall(Func<int, bool> shouldBlock)
    {
        var gate = new EngineGate();
        var callIndex = 0;

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
                var idx = Interlocked.Increment(ref callIndex);
                gate.EngineEntered.TrySetResult();
                if (shouldBlock(idx))
                {
                    return gate.Completion.Task;
                }
                return Task.FromResult(new DownloadResult { IsSuccess = true });
            });

        return gate;
    }

    [Fact]
    public async Task AddAsync_CreatesDownloadTask_SavesToRepository()
    {
        SetupEngineSuccess();

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);
        var request = CreateRequest();

        var task = await manager.AddAsync(request, CancellationToken.None);

        Assert.NotNull(task);
        Assert.Equal("123_456", task.VideoId);
        Assert.Equal("Test Video", task.Title);
        Assert.Equal("720p", task.Quality);
        Assert.Equal("mp4", task.Format);

        _repositoryMock.Verify(
            r => r.SaveAsync(It.Is<DownloadTask>(t =>
                t.VideoId == "123_456" &&
                t.Title == "Test Video")),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_ResolvesSourceUrl_WhenSourceUrlEmpty()
    {
        var resolvedUrl = "https://resolved-url.com/video.mp4";

        _downloadProviderMock
            .Setup(d => d.GetAvailableDownloadsAsync(
                It.IsAny<Video>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DownloadOption>
            {
                new DownloadOption
                {
                    Quality = "720p",
                    Format = "mp4",
                    SourceUrl = resolvedUrl
                }
            });

        _engineMock.Setup(e => e.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadResult { IsSuccess = true });

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);
        var request = CreateRequest(sourceUrl: null);

        await manager.AddAsync(request, CancellationToken.None);

        _engineMock.Verify(e => e.DownloadAsync(
            resolvedUrl,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _downloadProviderMock.Verify(
            d => d.GetAvailableDownloadsAsync(
                It.Is<Video>(v => v.Id == "123_456"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_CompletesSuccessfully_SetsCompletedStatus()
    {
        SetupEngineSuccess("final_video.mp4");

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(completed, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Equal(completed, result);

        var completedTask = await completed;
        Assert.Equal(DownloadStatus.Completed, completedTask.Status);
        Assert.Equal("final_video.mp4", completedTask.DestinationPath);
    }

    [Fact]
    public async Task AddAsync_EngineFailsAfterMaxRetries_SetsFailedStatus()
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
                    ErrorMessage = "Network error",
                    ErrorType = ErrorType.NetworkError
                });
            });

        var manager = CreateManager(retryCount: 1);
        var (completed, failed) = SubscribeToEvents(manager);

        await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(failed, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Equal(failed, result);

        var failedTask = await failed;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
        Assert.Equal("Network error", failedTask.ErrorMessage);
        Assert.Equal(1, failedTask.RetryCount);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task AddAsync_EngineReturnsError_RetriesWithExponentialBackoff()
    {
        var timestamps = new ConcurrentBag<DateTimeOffset>();
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
                timestamps.Add(DateTimeOffset.UtcNow);
                return Task.FromResult(new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Timeout",
                    ErrorType = ErrorType.Timeout
                });
            });

        var manager = CreateManager(retryCount: 2);
        var (completed, failed) = SubscribeToEvents(manager);

        await manager.AddAsync(CreateRequest(), CancellationToken.None);

        var result = await Task.WhenAny(failed, Task.Delay(TimeSpan.FromSeconds(20)));
        Assert.Equal(failed, result);

        var failedTask = await failed;
        Assert.Equal(DownloadStatus.Failed, failedTask.Status);
        Assert.Equal(2, failedTask.RetryCount);
        Assert.Equal(3, timestamps.Count);
    }

    [Fact]
    public async Task PauseAsync_SetsPausedStatus()
    {
        var gate = SetupEngineBlocking();

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);
        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.PauseAsync(task.Id, CancellationToken.None);

        Assert.Equal(DownloadStatus.Paused, task.Status);

        _repositoryMock.Verify(
            r => r.UpdateStatusAsync(task.Id, DownloadStatus.Paused),
            Times.Once);

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task ResumeAsync_SetsQueuedStatus()
    {
        var firstGate = SetupEngineBlocking();

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);
        await firstGate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.PauseAsync(task.Id, CancellationToken.None);
        Assert.Equal(DownloadStatus.Paused, task.Status);

        var secondGate = new TaskCompletionSource<DownloadResult>();
        _engineMock.Setup(e => e.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()))
            .Returns(() => secondGate.Task);

        await manager.ResumeAsync(task.Id, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.UpdateStatusAsync(task.Id, DownloadStatus.Queued),
            Times.Once);

        secondGate.TrySetCanceled();
    }

    [Fact]
    public async Task CancelAsync_SetsCancelledStatus()
    {
        var gate = SetupEngineBlocking();

        var manager = CreateManager();
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);
        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.CancelAsync(task.Id, CancellationToken.None);

        Assert.Equal(DownloadStatus.Cancelled, task.Status);

        _repositoryMock.Verify(
            r => r.UpdateStatusAsync(task.Id, DownloadStatus.Cancelled),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WithDeletePartOnCancel_DeletesPartFile()
    {
        var destPath = Path.Combine(Path.GetTempPath(), $"cancel_test_{Guid.NewGuid():N}.mp4");
        var partPath = destPath + ".part";
        _tempFiles.Add(destPath);

        File.WriteAllText(partPath, "partial content");

        var gate = SetupEngineBlocking();

        var manager = CreateManager(deletePartOnCancel: true);
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(destinationPath: destPath), CancellationToken.None);
        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.CancelAsync(task.Id, CancellationToken.None);

        Assert.False(File.Exists(partPath));

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task CancelAsync_WithoutDeletePartOnCancel_KeepsPartFile()
    {
        var destPath = Path.Combine(Path.GetTempPath(), $"cancel_keep_test_{Guid.NewGuid():N}.mp4");
        var partPath = destPath + ".part";
        _tempFiles.Add(destPath);

        File.WriteAllText(partPath, "partial content");

        var gate = SetupEngineBlocking();

        var manager = CreateManager(deletePartOnCancel: false);
        var (completed, failed) = SubscribeToEvents(manager);

        var task = await manager.AddAsync(CreateRequest(destinationPath: destPath), CancellationToken.None);
        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.CancelAsync(task.Id, CancellationToken.None);

        Assert.True(File.Exists(partPath));

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task RecoverIncompleteDownloadsAsync_MovesDownloadingToQueued()
    {
        var downloadingTask = new DownloadTask
        {
            Id = "dl-1",
            VideoId = "111",
            Title = "Downloading Video",
            SourceUrl = "https://example.com/video.mp4",
            DestinationPath = Path.Combine(Path.GetTempPath(), "rec_dl.mp4"),
            Status = DownloadStatus.Downloading
        };

        var completedTask = new DownloadTask
        {
            Id = "dl-2",
            VideoId = "222",
            Title = "Completed Video",
            Status = DownloadStatus.Completed
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<DownloadTask> { downloadingTask, completedTask });

        var gate = SetupEngineBlocking();

        var manager = CreateManager();

        await manager.RecoverIncompleteDownloadsAsync();

        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(DownloadStatus.Downloading, downloadingTask.Status);
        Assert.Equal(DownloadStatus.Completed, completedTask.Status);

        _repositoryMock.Verify(
            r => r.UpdateStatusAsync("dl-1", DownloadStatus.Queued),
            Times.Once);

        Assert.Contains(manager.Downloads, d => d.Id == "dl-1");
        Assert.DoesNotContain(manager.Downloads, d => d.Id == "dl-2");

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task RecoverIncompleteDownloadsAsync_MovesRetryingToQueued()
    {
        var retryingTask = new DownloadTask
        {
            Id = "rt-1",
            VideoId = "333",
            Title = "Retrying Video",
            SourceUrl = "https://example.com/video.mp4",
            DestinationPath = Path.Combine(Path.GetTempPath(), "rec_rt.mp4"),
            Status = DownloadStatus.Retrying,
            RetryCount = 2
        };

        var resolvingTask = new DownloadTask
        {
            Id = "rs-1",
            VideoId = "444",
            Title = "Resolving Video",
            SourceUrl = "https://example.com/video2.mp4",
            DestinationPath = Path.Combine(Path.GetTempPath(), "rec_rs.mp4"),
            Status = DownloadStatus.Resolving
        };

        _repositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<DownloadTask> { retryingTask, resolvingTask });

        var gate = SetupEngineBlocking();

        var manager = CreateManager();

        await manager.RecoverIncompleteDownloadsAsync();

        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(DownloadStatus.Downloading, retryingTask.Status);
        Assert.Equal(DownloadStatus.Downloading, resolvingTask.Status);

        _repositoryMock.Verify(
            r => r.UpdateStatusAsync("rt-1", DownloadStatus.Queued),
            Times.Once);
        _repositoryMock.Verify(
            r => r.UpdateStatusAsync("rs-1", DownloadStatus.Queued),
            Times.Once);

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task ActiveDownloadsCount_ReturnsCorrectCount()
    {
        var gate = SetupEngineBlocking();

        var manager = CreateManager();

        Assert.Equal(0, manager.ActiveDownloadsCount);

        var task1 = await manager.AddAsync(CreateRequest(), CancellationToken.None);
        var task2 = await manager.AddAsync(CreateRequest(), CancellationToken.None);
        var task3 = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        await Task.Delay(500);

        Assert.Equal(3, manager.ActiveDownloadsCount);

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task RemoveAsync_DeletesFromRepository()
    {
        var gate = SetupEngineBlocking();

        var manager = CreateManager();
        var task = await manager.AddAsync(CreateRequest(), CancellationToken.None);

        await gate.EngineEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        await manager.RemoveAsync(task.Id, false, CancellationToken.None);

        _repositoryMock.Verify(r => r.DeleteAsync(task.Id), Times.Once);
        Assert.DoesNotContain(manager.Downloads, d => d.Id == task.Id);

        gate.Completion.TrySetCanceled();
    }

    [Fact]
    public async Task ConcurrencySemaphore_LimitsParallelDownloads()
    {
        var maxConcurrent = 2;
        var concurrentCount = 0;
        var maxObservedConcurrency = 0;
        var enteredEngineCount = 0;
        var allEnteredTcs = new TaskCompletionSource();
        var releaseTcs = new TaskCompletionSource();

        _engineMock.Setup(e => e.DownloadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long?>(),
            It.IsAny<long>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                var totalEntered = Interlocked.Increment(ref enteredEngineCount);

                int oldMax;
                do
                {
                    oldMax = Volatile.Read(ref maxObservedConcurrency);
                }
                while (current > oldMax &&
                    Interlocked.CompareExchange(ref maxObservedConcurrency, current, oldMax) != oldMax);

                if (totalEntered >= maxConcurrent)
                    allEnteredTcs.TrySetResult();

                await releaseTcs.Task;

                Interlocked.Decrement(ref concurrentCount);
                return new DownloadResult { IsSuccess = true };
            });

        var manager = CreateManager(maxConcurrentDownloads: maxConcurrent);

        var addTasks = Enumerable.Range(0, 5)
            .Select(_ => manager.AddAsync(CreateRequest(), CancellationToken.None))
            .ToList();

        await Task.WhenAll(addTasks);

        await Task.Delay(500);

        Assert.Equal(maxConcurrent, maxObservedConcurrency);
        Assert.Equal(maxConcurrent, concurrentCount);

        releaseTcs.TrySetResult();
        await Task.Delay(1000);

        Assert.Equal(0, concurrentCount);
    }
}
