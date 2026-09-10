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
        try
        {
            if (!IsWindowsAppRuntimeAvailable())
            {
                WriteStartupCrashLog(new DllNotFoundException(
                    "Windows App Runtime не установлен. Установите его: https://aka.ms/windowsappsdk/1.7/1.7.260224002/windowsappruntimeinstall-x64.exe"));

                MessageBox(IntPtr.Zero,
                    "Windows App Runtime не установлен.\n\n" +
                    "VK Video Desktop требует Windows App Runtime 1.7.\n\n" +
                    "Нажмите OK для открытия страницы загрузки.",
                    "VK Video Desktop — ошибка запуска",
                    0x00000010);

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        "https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads-archive")
                    { UseShellExecute = true });
                }
                catch { }
                return;
            }

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

    private static bool IsWindowsAppRuntimeAvailable()
    {
        try
        {
            var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var runtimeDll = Path.Combine(systemDir, "Microsoft.WindowsAppRuntime.dll");
            if (File.Exists(runtimeDll)) return true;

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dynamicDir = Path.Combine(localAppData, "Microsoft", "WindowsAppRuntime");
            if (Directory.Exists(dynamicDir)) return true;

            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows App Runtime");
            if (key != null)
            {
                var version = key.GetValue("Version")?.ToString();
                if (!string.IsNullOrEmpty(version)) return true;
            }

            using var keyWow = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Microsoft\Windows App Runtime");
            if (keyWow != null)
            {
                var version = keyWow.GetValue("Version")?.ToString();
                if (!string.IsNullOrEmpty(version)) return true;
            }

            return false;
        }
        catch
        {
            return false;
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
