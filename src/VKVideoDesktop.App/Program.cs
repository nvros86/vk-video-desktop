using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace VKVideoDesktop.App;

static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    static void Main(string[] args)
    {
        WriteDebugLog("Main() entered");
        try
        {
            WriteDebugLog("Calling InitializeComWrappers...");
            WinRT.ComWrappersSupport.InitializeComWrappers();
            WriteDebugLog("InitializeComWrappers done, starting Application...");
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                WriteDebugLog("Application.Start callback");
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            WriteDebugLog($"Exception: {ex.GetType().Name}: {ex.Message}");
            WriteStartupCrashLog(ex);

            var isRuntimeMissing = ex is DllNotFoundException
                || (ex.InnerException is DllNotFoundException)
                || ex.Message.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("WinRT", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Windows App Runtime", StringComparison.OrdinalIgnoreCase);

            if (isRuntimeMissing)
            {
                MessageBox(IntPtr.Zero,
                    "Windows App Runtime не установлен или повреждён.\n\n" +
                    "VK Video Desktop требует Windows App Runtime 1.7.\n\n" +
                    "Нажмите OK для открытия страницы загрузки.",
                    "VK Video Desktop — ошибка запуска",
                    0x00000010);

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "https://aka.ms/windowsappsdk/1.7/1.7.260224002/windowsappruntimeinstall-x64.exe")
                    { UseShellExecute = true });
                }
                catch { }
            }
            else
            {
                throw;
            }
        }
    }

    private static void WriteDebugLog(string message)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VKVideoDesktop", "log", "debug.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
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
                if (ex.InnerException.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Внутреннее (2): {ex.InnerException.InnerException.GetType().FullName}");
                    sb.AppendLine($"Сообщение: {ex.InnerException.InnerException.Message}");
                }
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
