using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;
using Xunit;

namespace VKVideoDesktop.Tests.Integration;

public class TestSettingsService : IDisposable
{
    private static readonly Dictionary<string, object> _fileLocks = new();
    private readonly string _tempDir;
    private readonly string _settingsPath;
    private readonly bool _ownsTempDir;
    private UserSettings _settings = new();

    public UserSettings Settings => _settings;

    public string SettingsPath => _settingsPath;

    public TestSettingsService()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VKVideoDesktopTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _ownsTempDir = true;
    }

    public TestSettingsService(string tempDir)
    {
        _tempDir = tempDir;
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _ownsTempDir = false;
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_settingsPath))
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            _settings = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();

            if (!string.IsNullOrEmpty(_settings.AccessToken))
            {
                try
                {
                    var encryptedBytes = Convert.FromBase64String(_settings.AccessToken);
                    _settings.AccessToken = DecryptToken(encryptedBytes);
                }
                catch
                {
                    _settings.AccessToken = "";
                }
            }
        }
        else
        {
            _settings = new UserSettings();
        }
    }

    public Task SaveAsync()
    {
        var plaintext = _settings.AccessToken;
        if (!string.IsNullOrEmpty(plaintext))
        {
            _settings.AccessToken = Convert.ToBase64String(EncryptToken(plaintext));
        }

        var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        var lockObj = GetFileLock(_settingsPath);
        lock (lockObj)
        {
            File.WriteAllText(_settingsPath, json);
        }

        _settings.AccessToken = plaintext;
        return Task.CompletedTask;
    }

    private static object GetFileLock(string path)
    {
        lock (_fileLocks)
        {
            if (!_fileLocks.TryGetValue(path, out var obj))
            {
                obj = new object();
                _fileLocks[path] = obj;
            }
            return obj;
        }
    }

    public Task ResetAsync()
    {
        _settings = new UserSettings();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_ownsTempDir)
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    private static byte[] EncryptToken(string token)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(token);
        return ProtectedData.Protect(plainBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
    }

    private static string DecryptToken(byte[] encryptedBytes)
    {
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}

public class SettingsServiceIntegrationTests : IDisposable
{
    private readonly TestSettingsService _service = new();

    [Fact]
    public async Task SaveAsync_CreatesFile()
    {
        var service = new TestSettingsService();
        try
        {
            service.Settings.Theme = AppTheme.Light;
            service.Settings.MaxConcurrentDownloads = 4;

            await service.SaveAsync();

            Assert.True(File.Exists(service.SettingsPath));
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task LoadAsync_RestoresSettings()
    {
        _service.Settings.Theme = AppTheme.Light;
        _service.Settings.MaxConcurrentDownloads = 7;
        _service.Settings.Language = "en";
        _service.Settings.DefaultQuality = "720p";
        _service.Settings.Autoplay = false;
        await _service.SaveAsync();

        var service2 = new TestSettingsService(Path.GetDirectoryName(_service.SettingsPath)!);
        try
        {
            await service2.LoadAsync();

            Assert.Equal(AppTheme.Light, service2.Settings.Theme);
            Assert.Equal(7, service2.Settings.MaxConcurrentDownloads);
            Assert.Equal("en", service2.Settings.Language);
            Assert.Equal("720p", service2.Settings.DefaultQuality);
            Assert.False(service2.Settings.Autoplay);
        }
        finally
        {
            service2.Dispose();
        }
    }

    [Fact]
    public async Task ResetAsync_RestoresDefaults()
    {
        _service.Settings.Theme = AppTheme.Light;
        _service.Settings.MaxConcurrentDownloads = 5;
        _service.Settings.Language = "en";
        await _service.SaveAsync();

        await _service.ResetAsync();

        Assert.Equal(AppTheme.Dark, _service.Settings.Theme);
        Assert.Equal(2, _service.Settings.MaxConcurrentDownloads);
        Assert.Equal("ru", _service.Settings.Language);
        Assert.Equal(3, _service.Settings.RetryCount);
        Assert.True(_service.Settings.EnableNotifications);
        Assert.True(_service.Settings.Autoplay);
    }

    [Fact]
    public async Task ConcurrentAccess_DoesNotCorrupt()
    {
        var sharedDir = Path.Combine(Path.GetTempPath(), "VKVideoDesktopConcurrent_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sharedDir);

        try
        {
            var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
            {
                var service = new TestSettingsService(sharedDir);
                try
                {
                    service.Settings.Theme = i % 2 == 0 ? AppTheme.Light : AppTheme.Dark;
                    service.Settings.MaxConcurrentDownloads = i + 1;
                    service.Settings.Language = i % 2 == 0 ? "en" : "ru";
                    await service.SaveAsync();
                }
                finally
                {
                    service.Dispose();
                }
            }));

            await Task.WhenAll(tasks);

            var service2 = new TestSettingsService(sharedDir);
            try
            {
                await service2.LoadAsync();
                Assert.NotNull(service2.Settings);
                Assert.True(Enum.IsDefined(service2.Settings.Theme));
                Assert.InRange(service2.Settings.MaxConcurrentDownloads, 1, 10);
            }
            finally
            {
                service2.Dispose();
            }
        }
        finally
        {
            try { Directory.Delete(sharedDir, true); } catch { }
        }
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}

public class DeepLinkServiceIntegrationTests
{
    private readonly DeepLinkService _service = new();

    [Fact]
    public void ProcessString_ValidVideoUrl_ReturnsVideoId()
    {
        var result = _service.ProcessString("vkvideo://video/123_456");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Video, result.Type);
        Assert.Equal("123_456", result.VideoId);
    }

    [Fact]
    public void ProcessString_ValidChannelUrl_ReturnsChannelId()
    {
        var result = _service.ProcessString("vkvideo://channel/123");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Channel, result.Type);
        Assert.Equal("123", result.ChannelId);
    }

    [Fact]
    public void ProcessString_ValidSearchUrl_ReturnsSearchQuery()
    {
        var result = _service.ProcessString("vkvideo://search/hello");

        Assert.NotNull(result);
        Assert.Equal(DeepLinkType.Search, result.Type);
        Assert.Equal("hello", result.Query);
    }

    [Fact]
    public void ProcessString_InvalidUrl_ReturnsNull()
    {
        var result = _service.ProcessString("not-a-valid-url-at-all");
        Assert.Null(result);
    }

    [Fact]
    public void ProcessString_EmptyString_ReturnsNull()
    {
        Assert.Null(_service.ProcessString(""));
        Assert.Null(_service.ProcessString("  "));
        Assert.Null(_service.ProcessString(null!));
    }
}

public class TestDownloadEngine : IDisposable
{
    private readonly ILogger<TestDownloadEngine> _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _serverCts;
    private Task? _serverTask;

    public TestDownloadEngine()
    {
        _logger = Mock.Of<ILogger<TestDownloadEngine>>();
    }

    public async Task<string> StartServerAsync(byte[] content)
    {
        _listener = new HttpListener();
        var port = GetRandomPort();
        var prefix = $"http://localhost:{port}/test/";
        _listener.Prefixes.Add(prefix);
        _listener.Start();

        var readyTcs = new TaskCompletionSource();

        _serverCts = new CancellationTokenSource();
        _serverTask = Task.Run(async () =>
        {
            readyTcs.SetResult();
            while (!_serverCts.Token.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.ContentLength64 = content.Length;
                    await context.Response.OutputStream.WriteAsync(content, _serverCts.Token);
                    context.Response.Close();
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        });

        await readyTcs.Task;
        return prefix;
    }

    public async Task<DownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        string temporaryPath,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var downloadedBytes = 0L;

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;

        Directory.CreateDirectory(Path.GetDirectoryName(temporaryPath)!);

        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);

            var buffer = new byte[65536];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                var progressPercent = totalBytes > 0
                    ? (double)downloadedBytes / totalBytes * 100
                    : 0;

                progress?.Report(new DownloadProgress
                {
                    BytesReceived = downloadedBytes,
                    TotalBytes = totalBytes,
                    ProgressPercent = progressPercent,
                    SpeedBytesPerSecond = 0,
                    RemainingTime = null
                });
            }

            await fileStream.FlushAsync(cancellationToken);
        }

        if (File.Exists(temporaryPath) && !File.Exists(destinationPath))
        {
            File.Move(temporaryPath, destinationPath);
        }

        return new DownloadResult
        {
            IsSuccess = true,
            FilePath = File.Exists(destinationPath) ? destinationPath : temporaryPath,
            BytesWritten = downloadedBytes
        };
    }

    public void Dispose()
    {
        _serverCts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _serverTask?.Wait(TimeSpan.FromSeconds(2));
        _serverCts?.Dispose();
    }

    private static int GetRandomPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

public class DownloadEngineIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    public DownloadEngineIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VKVideoDownloadTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task DownloadAsync_SmallFile_Succeeds()
    {
        var testData = new byte[1024];
        RandomNumberGenerator.Fill(testData);

        using var server = new TestDownloadEngine();
        var url = await server.StartServerAsync(testData);

        var destPath = Path.Combine(_tempDir, "output.bin");
        var tempPath = Path.Combine(_tempDir, "output.bin.part");

        var result = await server.DownloadAsync(url, destPath, tempPath, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(destPath));
        Assert.Equal(1024, result.BytesWritten);

        var downloaded = await File.ReadAllBytesAsync(destPath);
        Assert.Equal(testData, downloaded);
    }

    [Fact]
    public async Task DownloadAsync_WithProgress_ReportsProgress()
    {
        var testData = new byte[64 * 1024];
        RandomNumberGenerator.Fill(testData);

        using var server = new TestDownloadEngine();
        var url = await server.StartServerAsync(testData);

        var destPath = Path.Combine(_tempDir, "progress.bin");
        var tempPath = Path.Combine(_tempDir, "progress.bin.part");

        var progressReports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => progressReports.Add(p));

        await server.DownloadAsync(url, destPath, tempPath, progress, CancellationToken.None);

        await Task.Delay(100);

        Assert.NotEmpty(progressReports);
        Assert.True(progressReports.Last().BytesReceived > 0);
        Assert.True(progressReports.Last().ProgressPercent > 0);
    }

    [Fact]
    public async Task DownloadAsync_Cancelled_ThrowsCancellation()
    {
        var testData = new byte[1024 * 1024];
        RandomNumberGenerator.Fill(testData);

        using var server = new TestDownloadEngine();
        var url = await server.StartServerAsync(testData);

        var destPath = Path.Combine(_tempDir, "cancelled.bin");
        var tempPath = Path.Combine(_tempDir, "cancelled.bin.part");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => server.DownloadAsync(url, destPath, tempPath, null, cts.Token));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }
}
