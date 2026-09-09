using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Infrastructure.Download;

public sealed class DownloadEngine : IDownloadEngine
{
    private readonly ILogger<DownloadEngine> _logger;
    private const int BufferSize = 65536; // 64 KB
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
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var downloadedBytes = 0L;
        var stopwatch = Stopwatch.StartNew();
        var speedSamples = new List<(long bytes, double elapsedMs)>();

        try
        {
            ValidateUrl(sourceUrl);

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

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                var elapsedMs = stopwatch.ElapsedMilliseconds;
                if (elapsedMs > 0)
                {
                    speedSamples.Add((bytesRead, 1));
                    if (speedSamples.Count > 20)
                        speedSamples.RemoveAt(0);
                }

                var speed = speedSamples.Count > 0
                    ? speedSamples.Sum(s => s.bytes) / (stopwatch.Elapsed.TotalSeconds)
                    : 0;

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

            _logger.LogInformation(
                "Download complete: {Path} ({Bytes} bytes)",
                temporaryPath, downloadedBytes);

            return new DownloadResult
            {
                IsSuccess = true,
                FilePath = temporaryPath,
                BytesWritten = downloadedBytes
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled: {Path}", temporaryPath);
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
            _logger.LogError(ex, "Network error during download: {Path}", temporaryPath);
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
            _logger.LogError(ex, "Unexpected error during download: {Path}", temporaryPath);
            return new DownloadResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ErrorType = ErrorType.UnknownError,
                BytesWritten = downloadedBytes
            };
        }
    }

    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid URL: {url}");

        if (uri.Scheme != "https" && uri.Scheme != "http")
            throw new ArgumentException($"Unsupported URL scheme: {uri.Scheme}");

        if (uri.IsFile || uri.IsLoopback)
            throw new ArgumentException("File and loopback URLs are not allowed");
    }
}
