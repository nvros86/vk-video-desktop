using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;

using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class DownloadEngine : IDownloadEngine
{
    private readonly ILogger<DownloadEngine> _logger;
    private readonly ConcurrentQueue<(DateTime time, long bytes)> _speedSamples = new();
    private const int BufferSize = 65536;
    private const int MaxRedirects = 10;

    public DownloadEngine(ILogger<DownloadEngine> logger)
    {
        _logger = logger;
    }

    public async Task<DownloadResult> DownloadAsync(
        string sourceUrl,
        string destinationPath,
        string temporaryPath,
        long? totalBytes,
        long speedLimitBytesPerSecond,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var downloadedBytes = 0L;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ValidateUrl(sourceUrl);
            _logger.LogInformation("[DownloadEngine] Starting download: {FileName}", Path.GetFileName(destinationPath));

            if (!HasEnoughDiskSpace(destinationPath, totalBytes))
            {
                _logger.LogWarning("[DownloadEngine] Insufficient disk space for: {Path}", destinationPath);
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Недостаточно свободного места на диске",
                    ErrorType = ErrorType.InsufficientSpace,
                    BytesWritten = 0
                };
            }

            var directory = Path.GetDirectoryName(temporaryPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "VKVideoDesktop/1.0");

            if (File.Exists(temporaryPath))
            {
                downloadedBytes = new FileInfo(temporaryPath).Length;
                if (downloadedBytes > 0 && totalBytes.HasValue && downloadedBytes >= totalBytes.Value)
                {
                    downloadedBytes = 0;
                    File.Delete(temporaryPath);
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);

            if (downloadedBytes > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(downloadedBytes, null);
                _logger.LogInformation("Resuming download from byte {Offset}", downloadedBytes);
            }

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (downloadedBytes > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                _logger.LogWarning("Server returned {StatusCode} instead of 206. Restarting download.",
                    response.StatusCode);
                downloadedBytes = 0;
                File.Delete(temporaryPath);
            }

            response.EnsureSuccessStatusCode();

            if (!totalBytes.HasValue && response.Content.Headers.ContentLength.HasValue)
            {
                totalBytes = response.Content.Headers.ContentLength.Value + downloadedBytes;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(
                temporaryPath,
                downloadedBytes > 0 ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true);

            var buffer = new byte[BufferSize];
            int bytesRead;
            bool diskSpaceExhausted = false;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (downloadedBytes % (1024 * 1024) < bytesRead)
                {
                    var drivePath = Path.GetPathRoot(temporaryPath) ?? "C:\\";
                    var drive = new DriveInfo(drivePath);
                    if (drive.AvailableFreeSpace < 1024 * 1024)
                    {
                        _logger.LogWarning("Disk space critically low during download");
                        diskSpaceExhausted = true;
                        break;
                    }
                }

                var speed = CalculateSpeed(bytesRead);

                if (speedLimitBytesPerSecond > 0 && speed > speedLimitBytesPerSecond)
                {
                    var delayMs = (int)((bytesRead / (double)speedLimitBytesPerSecond) * 1000);
                    if (delayMs > 0)
                        await Task.Delay(delayMs, cancellationToken);
                }

                var progressPercent = totalBytes > 0
                    ? (double)downloadedBytes / totalBytes.Value * 100
                    : 0;

                TimeSpan? remainingTime = null;
                if (speed > 0 && totalBytes.HasValue)
                {
                    var remainingBytes = totalBytes.Value - downloadedBytes;
                    remainingTime = TimeSpan.FromSeconds(remainingBytes / speed);
                }

                progress?.Report(new DownloadProgress
                {
                    BytesReceived = downloadedBytes,
                    TotalBytes = totalBytes,
                    ProgressPercent = progressPercent,
                    SpeedBytesPerSecond = speed,
                    RemainingTime = remainingTime
                });
            }

            await fileStream.FlushAsync(cancellationToken);

            if (diskSpaceExhausted)
            {
                _logger.LogWarning("Download incomplete due to insufficient disk space: {Path}", destinationPath);
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Недостаточно свободного места на диске",
                    ErrorType = ErrorType.InsufficientSpace,
                    BytesWritten = downloadedBytes
                };
            }

            if (totalBytes.HasValue && totalBytes.Value > 0 && downloadedBytes < totalBytes.Value)
            {
                _logger.LogWarning("Incomplete download: expected {Expected} bytes, got {Actual} bytes",
                    totalBytes.Value, downloadedBytes);
                return new DownloadResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Неполная загрузка: получено {downloadedBytes} из {totalBytes.Value} байт",
                    ErrorType = ErrorType.UnknownError,
                    BytesWritten = downloadedBytes
                };
            }

            if (File.Exists(temporaryPath) && !File.Exists(destinationPath))
            {
                File.Move(temporaryPath, destinationPath);
            }

            _logger.LogInformation(
                "Download complete: {Path} ({Bytes} bytes)",
                temporaryPath, downloadedBytes);

            return new DownloadResult
            {
                IsSuccess = true,
                FilePath = File.Exists(destinationPath) ? destinationPath : temporaryPath,
                BytesWritten = downloadedBytes
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[DownloadEngine] Cancelled: {Path}", temporaryPath);
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = "Download cancelled",
                ErrorType = ErrorType.Cancelled,
                BytesWritten = downloadedBytes
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[DownloadEngine] Network error: {Path}", temporaryPath);
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorType = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound => ErrorType.VideoUnavailable,
                    System.Net.HttpStatusCode.Forbidden => ErrorType.AccessDenied,
                    System.Net.HttpStatusCode.Unauthorized => ErrorType.AuthenticationError,
                    System.Net.HttpStatusCode.TooManyRequests => ErrorType.RateLimited,
                    System.Net.HttpStatusCode.RequestTimeout => ErrorType.Timeout,
                    >= System.Net.HttpStatusCode.InternalServerError => ErrorType.ServerError,
                    _ => ErrorType.NetworkError
                },
                BytesWritten = downloadedBytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DownloadEngine] Unexpected error: {Path}", temporaryPath);
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorType = ErrorType.UnknownError,
                BytesWritten = downloadedBytes
            };
        }
    }

    private double CalculateSpeed(long bytesRead)
    {
        var now = DateTime.UtcNow;
        _speedSamples.Enqueue((now, bytesRead));

        while (_speedSamples.TryPeek(out var oldest) &&
               (now - oldest.time).TotalSeconds > 3)
        {
            _speedSamples.TryDequeue(out _);
        }

        if (_speedSamples.IsEmpty) return 0;

        var totalBytes = _speedSamples.Sum(s => s.bytes);
        var timeSpan = (now - _speedSamples.First().time).TotalSeconds;

        return timeSpan > 0 ? totalBytes / timeSpan : 0;
    }

    private static bool HasEnoughDiskSpace(string path, long? requiredBytes)
    {
        if (!requiredBytes.HasValue) return true;

        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
            var safetyMargin = 1024 * 1024;
            return drive.AvailableFreeSpace > requiredBytes.Value + safetyMargin;
        }
        catch
        {
            return true;
        }
    }

    private static void ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid URL format");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Only HTTPS URLs are supported");
    }
}
