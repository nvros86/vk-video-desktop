using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();
                WriteDebugLog("InitializeComWrappers done");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"InitializeComWrappers FAILED (continuing): {ex.GetType().Name}: {ex.Message}");
            }
            WriteDebugLog("starting Application...");

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                WriteDebugLog("Application.Start callback");
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                WriteDebugLog("Creating App and building DI...");
                var app = new App();
                WriteDebugLog("DI ready, creating window synchronously...");

                MainWindow? window = null;
                bool windowOk = false;

                try
                {
                    WriteDebugLog("Step 1: new MainWindow()...");
                    window = new MainWindow();
                    WriteDebugLog("Step 1 OK");
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"Step 1 FAILED (managed): {ex.GetType().Name}: {ex.Message}");
                }

                if (window == null)
                {
                    WriteDebugLog("MainWindow failed, trying plain Window...");
                    try
                    {
                        window = new MainWindow();
                        WriteDebugLog("Second MainWindow attempt OK");
                    }
                    catch
                    {
                        WriteDebugLog("Both MainWindow attempts failed, creating plain Window");
                    }
                }

                if (window != null)
                {
                    try
                    {
                        WriteDebugLog("Step 2: SetupUI...");
                        window.SetupUI();
                        WriteDebugLog("Step 2 OK");

                        WriteDebugLog("Step 3: Title...");
                        window.Title = "VK Video Desktop";
                        WriteDebugLog("Step 3 OK");

                        WriteDebugLog("Step 4: Activate...");
                        window.Activate();
                        WriteDebugLog("Step 4 OK - MainWindow activated!");
                        windowOk = true;
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"Setup/Activate FAILED: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                if (!windowOk)
                {
                    WriteDebugLog("All window attempts failed. Creating minimal Window...");
                    try
                    {
                        var w = new Microsoft.UI.Xaml.Window();
                        w.Title = "VK Video Desktop";
                        w.Content = new TextBlock
                        {
                            Text = "VK Video Desktop is loading...\nIf you see this, MainWindow creation failed.",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        w.Activate();
                        WriteDebugLog("Minimal Window activated!");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"Minimal Window FAILED: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                WriteDebugLog("Starting async host + settings...");
                if (window != null)
                    _ = app.StartupAsync(window);
                WriteDebugLog("Application.Start callback done");
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
