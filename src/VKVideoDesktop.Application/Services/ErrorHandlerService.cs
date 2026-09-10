using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;

namespace VKVideoDesktop.Application.Services;

public sealed class ErrorHandlerService
{
    private readonly ILogger<ErrorHandlerService> _logger;

    public ErrorHandlerService(ILogger<ErrorHandlerService> logger)
    {
        _logger = logger;
    }

    public string GetUserFriendlyMessage(ErrorType errorType, string? details = null)
    {
        return errorType switch
        {
            ErrorType.NetworkError => "Ошибка сети. Проверьте подключение к интернету.",
            ErrorType.AuthenticationError => "Ошибка авторизации. Пожалуйста, войдите в VK снова.",
            ErrorType.AccessDenied => "Доступ запрещён. У вас нет прав для просмотра этого контента.",
            ErrorType.VideoUnavailable => "Видео недоступно. Возможно, оно было удалено или скрыто.",
            ErrorType.DownloadUnavailable => "Скачивание недоступно для этого видео.",
            ErrorType.RateLimited => "Слишком много запросов. Подождите немного и попробуйте снова.",
            ErrorType.ServerError => "Ошибка сервера VK. Попробуйте позже.",
            ErrorType.StorageError => "Ошибка сохранения. Проверьте свободное место на диске.",
            ErrorType.InsufficientSpace => "Недостаточно свободного места на диске.",
            ErrorType.Timeout => "Превышено время ожидания. Проверьте скорость подключения.",
            ErrorType.Cancelled => "Операция отменена.",
            _ => string.IsNullOrEmpty(details)
                ? "Произошла неизвестная ошибка. Попробуйте снова."
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
