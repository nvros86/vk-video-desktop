using System.Collections.Concurrent;

namespace VKVideoDesktop.Core.Metrics;

public sealed class AppMetrics
{
    private long _apiCalls;
    private long _apiFailures;
    private long _apiTotalDurationMs;
    private long _downloadsStarted;
    private long _downloadsCompleted;
    private long _downloadsFailed;
    private long _downloadsCancelled;
    private long _retries;
    private readonly ConcurrentDictionary<string, long> _failureReasons = new();
    private readonly ConcurrentDictionary<string, long> _retryReasons = new();
    private readonly ConcurrentDictionary<string, long> _apiMethodCalls = new();

    public void TrackApiCall(string method, TimeSpan duration, bool success)
    {
        Interlocked.Increment(ref _apiCalls);
        Interlocked.Add(ref _apiTotalDurationMs, (long)duration.TotalMilliseconds);
        if (!success) Interlocked.Increment(ref _apiFailures);
        _apiMethodCalls.AddOrUpdate(method, 1, (_, count) => count + 1);
    }

    public void DownloadStarted()
    {
        Interlocked.Increment(ref _downloadsStarted);
    }

    public void DownloadCompleted()
    {
        Interlocked.Increment(ref _downloadsCompleted);
    }

    public void DownloadFailed(string? reason)
    {
        Interlocked.Increment(ref _downloadsFailed);
        if (!string.IsNullOrEmpty(reason))
            _failureReasons.AddOrUpdate(reason, 1, (_, count) => count + 1);
    }

    public void DownloadCancelled()
    {
        Interlocked.Increment(ref _downloadsCancelled);
    }

    public void TrackRetry(string? reason)
    {
        Interlocked.Increment(ref _retries);
        if (!string.IsNullOrEmpty(reason))
            _retryReasons.AddOrUpdate(reason, 1, (_, count) => count + 1);
    }

    public MetricsSnapshot GetSnapshot()
    {
        return new MetricsSnapshot
        {
            ApiCalls = Interlocked.Read(ref _apiCalls),
            ApiFailures = Interlocked.Read(ref _apiFailures),
            ApiAvgDurationMs = Interlocked.Read(ref _apiCalls) > 0
                ? (double)Interlocked.Read(ref _apiTotalDurationMs) / Interlocked.Read(ref _apiCalls)
                : 0,
            DownloadsStarted = Interlocked.Read(ref _downloadsStarted),
            DownloadsCompleted = Interlocked.Read(ref _downloadsCompleted),
            DownloadsFailed = Interlocked.Read(ref _downloadsFailed),
            DownloadsCancelled = Interlocked.Read(ref _downloadsCancelled),
            Retries = Interlocked.Read(ref _retries),
            FailureReasons = new Dictionary<string, long>(_failureReasons),
            RetryReasons = new Dictionary<string, long>(_retryReasons),
            ApiMethodCalls = new Dictionary<string, long>(_apiMethodCalls)
        };
    }
}

public sealed class MetricsSnapshot
{
    public long ApiCalls { get; init; }
    public long ApiFailures { get; init; }
    public double ApiAvgDurationMs { get; init; }
    public long DownloadsStarted { get; init; }
    public long DownloadsCompleted { get; init; }
    public long DownloadsFailed { get; init; }
    public long DownloadsCancelled { get; init; }
    public long Retries { get; init; }
    public Dictionary<string, long> FailureReasons { get; init; } = new();
    public Dictionary<string, long> RetryReasons { get; init; } = new();
    public Dictionary<string, long> ApiMethodCalls { get; init; } = new();
}
