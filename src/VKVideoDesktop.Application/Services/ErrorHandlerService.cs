using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;

namespace VKVideoDesktop.Application.Services;

public sealed class ErrorHandlerService
{
    private readonly ILogger<ErrorHandlerService> _logger;
    private readonly LocalizationService _localization;

    public ErrorHandlerService(ILogger<ErrorHandlerService> logger, LocalizationService localization)
    {
        _logger = logger;
        _localization = localization;
    }

    public string GetUserFriendlyMessage(ErrorType errorType, string? details = null)
    {
        return errorType switch
        {
            ErrorType.NetworkError => _localization["NetworkError"],
            ErrorType.AuthenticationError => _localization["AuthError"],
            ErrorType.AccessDenied => _localization["ErrorAccessDenied"],
            ErrorType.VideoUnavailable => _localization["VideoUnavailable"],
            ErrorType.DownloadUnavailable => _localization["ErrorDownloadUnavailable"],
            ErrorType.RateLimited => _localization["ErrorRateLimit"],
            ErrorType.ServerError => _localization["ServerError"],
            ErrorType.StorageError => _localization["StorageError"],
            ErrorType.InsufficientSpace => _localization["ErrorNoSpace"],
            ErrorType.Timeout => _localization["ErrorTimeout"],
            ErrorType.Cancelled => _localization["ErrorCancelled"],
            _ => string.IsNullOrEmpty(details)
                ? _localization["ErrorUnknown"]
                : $"Ошибка: {details}"
        };
    }

    public void LogError(ErrorType errorType, string? details = null, Exception? exception = null)
    {
        var message = $"[{errorType}] {details ?? "No details"}";
        if (exception != null)
            _logger.LogError(exception, "{Message}", message);
        else
            _logger.LogWarning("{Message}", message);
    }
}
