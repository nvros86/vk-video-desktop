using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Tests.Unit;

public class ModelTests
{
    [Fact]
    public void DownloadTask_DefaultValues_AreCorrect()
    {
        var task = new DownloadTask();

        Assert.False(string.IsNullOrEmpty(task.Id));
        Assert.Equal(DownloadStatus.Queued, task.Status);
        Assert.Equal(0, task.DownloadedBytes);
        Assert.Equal(0, task.Progress);
        Assert.Equal(0, task.RetryCount);
        Assert.True(task.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void DownloadOption_CreatesCorrectly()
    {
        var option = new DownloadOption
        {
            VideoId = "123_456",
            Quality = "720p",
            Format = "mp4",
            Size = 1024 * 1024 * 100,
            IsAvailable = true
        };

        Assert.Equal("123_456", option.VideoId);
        Assert.Equal("720p", option.Quality);
        Assert.Equal("mp4", option.Format);
        Assert.Equal(1024 * 1024 * 100, option.Size);
        Assert.True(option.IsAvailable);
    }

    [Fact]
    public void Video_CreatesCorrectly()
    {
        var video = new Video
        {
            Id = "123_456",
            Title = "Test Video",
            Duration = TimeSpan.FromMinutes(5),
            ViewCount = 1000,
            PublishedAt = DateTime.UtcNow.AddDays(-1)
        };

        Assert.Equal("123_456", video.Id);
        Assert.Equal("Test Video", video.Title);
        Assert.Equal(TimeSpan.FromMinutes(5), video.Duration);
        Assert.Equal(1000, video.ViewCount);
    }

    [Fact]
    public void UserSettings_DefaultDownloadFolder_IsCorrect()
    {
        var settings = new UserSettings();

        Assert.Contains("Videos", settings.DownloadFolder);
        Assert.Contains("VK Video", settings.DownloadFolder);
        Assert.Equal(2, settings.MaxConcurrentDownloads);
        Assert.Equal(3, settings.RetryCount);
        Assert.True(settings.EnableNotifications);
        Assert.True(settings.Autoplay);
    }

    [Fact]
    public void DownloadRequest_CreatesCorrectly()
    {
        var option = new DownloadOption
        {
            VideoId = "123_456",
            Quality = "1080p",
            Format = "mp4"
        };

        var request = new DownloadRequest
        {
            VideoId = "123_456",
            Title = "Test",
            Option = option,
            DestinationPath = @"C:\Videos\test.mp4"
        };

        Assert.Equal("123_456", request.VideoId);
        Assert.Equal("Test", request.Title);
        Assert.Equal("1080p", request.Option.Quality);
        Assert.Equal(@"C:\Videos\test.mp4", request.DestinationPath);
    }

    [Theory]
    [InlineData(DownloadStatus.Queued, true)]
    [InlineData(DownloadStatus.Downloading, true)]
    [InlineData(DownloadStatus.Paused, true)]
    [InlineData(DownloadStatus.Completed, false)]
    [InlineData(DownloadStatus.Failed, false)]
    [InlineData(DownloadStatus.Cancelled, false)]
    public void DownloadStatus_IsActive_ReturnsCorrectly(DownloadStatus status, bool expected)
    {
        var isActive = status == DownloadStatus.Queued
            || status == DownloadStatus.Downloading
            || status == DownloadStatus.Resolving
            || status == DownloadStatus.Retrying;

        Assert.Equal(expected, isActive);
    }
}
