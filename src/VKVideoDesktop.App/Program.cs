using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace VKVideoDesktop.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            WriteStartupCrashLog(ex);
            throw;
        }
    }

    private static void WriteStartupCrashLog(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VKVideoDesktop", "log");
            Directory.CreateDirectory(logDir);

            var crashPath = Path.Combine(logDir, $"startup-crash-{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== VK Video Desktop Startup Crash ===");
            sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Версия ОС: {Environment.OSVersion}");
            sb.AppendLine($"64-bit: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"CLR: {Environment.Version}");
            sb.AppendLine($"Пользователь: {Environment.UserName}");
            sb.AppendLine();
            sb.AppendLine($"Исключение: {ex.GetType().FullName}");
            sb.AppendLine($"Сообщение: {ex.Message}");
            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Внутреннее: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"Сообщение: {ex.InnerException.Message}");
            }
            sb.AppendLine();
            sb.AppendLine($"Стек:");
            sb.AppendLine(ex.StackTrace);
            File.WriteAllText(crashPath, sb.ToString());
        }
        catch
        {
        }
    }
}
