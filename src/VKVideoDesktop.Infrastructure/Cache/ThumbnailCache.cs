using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.Infrastructure.Cache;

public sealed class ThumbnailCache : IThumbnailCache
{
    private readonly string _cacheDirectory;
    private readonly ILogger<ThumbnailCache> _logger;

    public ThumbnailCache(ILogger<ThumbnailCache> logger)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDirectory = Path.Combine(appData, "VKVideoDesktop", "Cache", "Thumbnails");
        Directory.CreateDirectory(_cacheDirectory);
        _logger = logger;
    }

    public Task<string?> GetCachedPathAsync(string url)
    {
        var hash = ComputeHash(url);
        var filePath = Path.Combine(_cacheDirectory, hash);

        if (File.Exists(filePath))
            return Task.FromResult<string?>(filePath);

        return Task.FromResult<string?>(null);
    }

    public async Task<string> CacheAsync(string url, CancellationToken cancellationToken)
    {
        var hash = ComputeHash(url);
        var filePath = Path.Combine(_cacheDirectory, hash);

        if (File.Exists(filePath))
            return filePath;

        try
        {
            using var httpClient = new HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache thumbnail from {Url}", url);
            return string.Empty;
        }
    }

    public Task ClearCacheAsync()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, true);
            Directory.CreateDirectory(_cacheDirectory);
        }
        return Task.CompletedTask;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
