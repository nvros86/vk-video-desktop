using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace VKVideoDesktop.App;

static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop", "Logs", $"log-{DateTime.Now:yyyyMMdd}.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Main() entered");
        try
        {
            Log.Information("Calling InitializeComWrappers...");
            try
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();
                Log.Information("InitializeComWrappers done");
            }
            catch (Exception ex)
            {
                Log.Warning("InitializeComWrappers FAILED (continuing): {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
            }
            Log.Information("Starting Application...");

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                Log.Information("Application.Start callback");
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                Log.Information("Creating App and building DI...");
                var app = new App();
                Log.Information("DI ready, creating window synchronously...");

                MainWindow? window = null;
                bool windowOk = false;

                try
                {
                    Log.Information("Step 1: new MainWindow()...");
                    window = App.Services.GetRequiredService<MainWindow>();
                    Log.Information("Step 1 OK");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Step 1 FAILED (managed): {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
                }

                if (window == null)
                {
                    Log.Warning("MainWindow failed, trying second attempt...");
                    try
                    {
                        window = App.Services.GetRequiredService<MainWindow>();
                        Log.Information("Second MainWindow attempt OK");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Both MainWindow attempts failed");
                    }
                }

                if (window != null)
                {
                    try
                    {
                        Log.Information("Step 2: SetupUI...");
                        window.SetupUI();
                        Log.Information("Step 2 OK");

                        Log.Information("Step 3: Title...");
                        window.Title = "VK Video Desktop";
                        Log.Information("Step 3 OK");

                        Log.Information("Step 4: Activate...");
                        window.Activate();
                        Log.Information("Step 4 OK - MainWindow activated!");
                        windowOk = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Setup/Activate FAILED: {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
                    }
                }

                if (!windowOk)
                {
                    Log.Warning("All window attempts failed. Creating minimal Window...");
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
                        Log.Information("Minimal Window activated!");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Minimal Window FAILED: {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
                    }
                }

                Log.Information("Starting async host + settings...");
                if (windowOk && window != null)
                    _ = app.StartupAsync(window);
                Log.Information("Application.Start callback done");
            });
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exception in Main: {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
            Log.Fatal(ex, "Crash: {CrashType}", "Startup");

            var isRuntimeMissing = ex is DllNotFoundException
                || (ex.InnerException is DllNotFoundException)
                || ex.Message.Contains("WindowsAppRuntime", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("WinRT", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("Windows App Runtime", StringComparison.OrdinalIgnoreCase);

            if (isRuntimeMissing)
            {
                Log.Fatal("Windows App Runtime is missing or corrupted");
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
}
