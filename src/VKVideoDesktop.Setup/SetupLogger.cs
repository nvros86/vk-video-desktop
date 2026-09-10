namespace VKVideoDesktop.Setup;

public static class SetupLogger
{
    private static readonly object _lock = new();
    private static string _logPath = null!;

    public static string LogPath => _logPath;

    static SetupLogger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VKVideoDesktop", "Logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, $"setup-{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", message);
        if (ex != null)
        {
            Write("ERROR", $"  Тип: {ex.GetType().Name}");
            Write("ERROR", $"  Сообщение: {ex.Message}");
            if (ex.InnerException != null)
                Write("ERROR", $"  Внутренняя: {ex.InnerException.Message}");
            Write("ERROR", $"  Стек: {ex.StackTrace}");
        }
    }

    public static void Fatal(string message, Exception? ex = null)
    {
        Write("FATAL", message);
        if (ex != null)
        {
            Write("FATAL", $"  Тип: {ex.GetType().Name}");
            Write("FATAL", $"  Сообщение: {ex.Message}");
            if (ex.InnerException != null)
                Write("FATAL", $"  Внутренняя: {ex.InnerException.Message}");
            Write("FATAL", $"  Стек: {ex.StackTrace}");
        }
    }

    private static void Write(string level, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch
        {
        }
    }
}
