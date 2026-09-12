using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Data.Database;
using VKVideoDesktop.Infrastructure.Cache;
using VKVideoDesktop.Infrastructure;
using Serilog;
using VKVideoDesktop.Infrastructure.Download;
using VKVideoDesktop.Infrastructure.VkApi;

namespace VKVideoDesktop.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly IHost _host;
    private MainWindow? _mainWindow;
    public static IServiceProvider Services { get; private set; } = null!;

    private const int WM_TRAYICON = 0x0400 + 1;
    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int NIM_ADD = 0x00;
    private const int NIM_DELETE = 0x02;
    private const int NIM_MODIFY = 0x01;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;
    private const int ID_TRAY_SHOW = 1001;
    private const int ID_TRAY_EXIT = 1002;

    private IntPtr _trayIconHandle;
    private IntPtr _windowHandle;
    private GCHandle _gchThis;
    private bool _isClosing;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA pnid);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WndProcDelegate _wndProcDelegate;

    public App()
    {
        Log.Information("=== Application Starting ===");
        Log.Information("OS: {OSVersion}, 64-bit: {Is64Bit}", Environment.OSVersion, Environment.Is64BitOperatingSystem);
        Log.Information("CLR: {CLRVersion}, BaseDir: {BaseDir}", Environment.Version, AppContext.BaseDirectory);
        Log.Information("App constructor - InitializeComponent...");
        InitializeComponent();
        Log.Information("App constructor - done, starting DI...");

        _wndProcDelegate = WndProc;

        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VKVideoDesktop", "Logs");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogMetricsSnapshot();
                Log.Fatal(ex, "Crash: {CrashType}", "UnhandledException");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogMetricsSnapshot();
            Log.Fatal(e.Exception, "Crash: {CrashType}", "UnobservedTaskException");
            e.SetObserved();
        };

        Microsoft.UI.Xaml.Application.Current.UnhandledException += (_, e) =>
        {
            LogMetricsSnapshot();
            Log.Fatal(e.Exception, "Crash: {CrashType}", "WinUIUnhandledException");
            e.Handled = true;
        };

        var resourcesPath = AppContext.BaseDirectory;

        Log.Information("Building host...");
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop", "Logs", $"log-{DateTime.Now:yyyyMMdd}.txt");

                services.AddLogging(builder =>
                {
                    builder.AddSerilog(new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.File(logPath,
                            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            fileSizeLimitBytes: 10 * 1024 * 1024,
                            rollOnFileSizeLimit: true)
                        .CreateLogger());
                });

                services.AddSingleton<LocalizationService>();

                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAuthenticationService, VkAuthenticationService>();
                services.AddSingleton<IVideoProvider, VkVideoProvider>();
                services.AddSingleton<IDownloadEngine, DownloadEngine>();
                services.AddSingleton<IDownloadRepository, DownloadDatabase>();
                services.AddSingleton<IVideoDownloadProvider, VkVideoDownloadProvider>();
                services.AddSingleton<IDownloadManager, DownloadManager>();
                services.AddSingleton<IThumbnailCache, ThumbnailCache>();
                services.AddSingleton<IHistoryService, HistoryDatabase>();
                services.AddSingleton<IFavoritesService, FavoritesDatabase>();
                services.AddSingleton<IPlaylistService, PlaylistDatabase>();
                services.AddSingleton<IVKWebViewService, VKWebViewService>();
                services.AddSingleton<PlaybackService>();
                services.AddSingleton<ErrorHandlerService>();
                services.AddSingleton<DeepLinkService>();

                services.AddTransient<SearchService>();
                services.AddTransient<VideoService>();
                services.AddTransient<DownloadService>();

                services.AddSingleton<MainWindow>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<VideoViewModel>();
                services.AddSingleton<DownloadsViewModel>();

                services.AddSingleton<NotificationService>();
                services.AddSingleton<Core.Metrics.AppMetrics>();
            })
            .Build();

        Services = _host.Services;
        Log.Information("App constructor - host built, Services assigned");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Information("OnLaunched called");
    }

    public async Task StartupAsync(MainWindow? window = null, string? arguments = null)
    {
        _mainWindow = window;
        Log.Information("StartupAsync - entry");
        try
        {
            Log.Information("StartupAsync - starting host (background)...");
            await Task.Run(async () => await _host.StartAsync());
            Log.Information("StartupAsync - host started OK");

            Log.Information("StartupAsync - loading settings...");
            try
            {
                var settingsService = Services.GetRequiredService<ISettingsService>();
                await settingsService.LoadAsync();
                Log.Information("StartupAsync - settings loaded");
            }
            catch (Exception ex)
            {
                Log.Information("StartupAsync - settings err: {ExType}: {ExMessage}", ex.GetType().Name, ex.Message);
            }

            Log.Information("StartupAsync - subscribing to window events...");
            if (_mainWindow != null)
            {
                _mainWindow.Closed += OnMainWindowClosed;
                _mainWindow.InitServices();
                Log.Information("StartupAsync - services injected into MainWindow");
                _mainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    _mainWindow.NavigateToHome();
                });
            }

            Log.Information("StartupAsync - recovering incomplete downloads...");
            try
            {
                var settingsService = Services.GetRequiredService<ISettingsService>();
                if (settingsService.Settings.AutoResumeAfterStartup)
                {
                    var downloadManager = Services.GetRequiredService<IDownloadManager>();
                    await downloadManager.RecoverIncompleteDownloadsAsync();
                    Log.Information("StartupAsync - downloads recovered OK");
                }
                else
                {
                    Log.Information("StartupAsync - auto-resume disabled, skipping recovery");
                }
            }
            catch (Exception ex)
            {
                Log.Information("StartupAsync - download recovery err: {ExMessage}", ex.Message);
            }

            Log.Information("StartupAsync - init tray...");
            try { InitializeTrayIcon(); Log.Information("StartupAsync - tray OK"); }
            catch (Exception ex) { Log.Information("StartupAsync - tray err: {ExMessage}", ex.Message); }

            Log.Information("StartupAsync - ALL DONE!");

            if (arguments?.StartsWith("vkvideo://") == true || arguments?.StartsWith("vkvideo:") == true)
            {
                var deepLinkService = Services.GetRequiredService<DeepLinkService>();
                var result = deepLinkService.ProcessString(arguments);
                if (result != null && _mainWindow != null)
                {
                    _mainWindow.DispatcherQueue.TryEnqueue(async () =>
                    {
                        await _mainWindow.HandleDeepLinkAsync(result);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Information("StartupAsync FAILED: {ExType}: {ExMessage}\n{StackTrace}", ex.GetType().Name, ex.Message, ex.StackTrace);
        }
    }

    private void LogMetricsSnapshot()
    {
        try
        {
            if (Services?.GetService(typeof(Core.Metrics.AppMetrics)) is Core.Metrics.AppMetrics metrics)
            {
                var s = metrics.GetSnapshot();
                Log.Information("Metrics: API calls={ApiCalls}, failures={ApiFailures}, avg latency={AvgMs:N0}ms",
                    s.ApiCalls, s.ApiFailures, s.ApiAvgDurationMs);
                Log.Information("Metrics: Downloads started={Started}, completed={Completed}, failed={Failed}, cancelled={Cancelled}",
                    s.DownloadsStarted, s.DownloadsCompleted, s.DownloadsFailed, s.DownloadsCancelled);
                Log.Information("Metrics: Retries={Retries}", s.Retries);
            }
        }
        catch { }
    }

    private void OnMainWindowClosed(object? sender, WindowEventArgs args)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            RemoveTrayIcon();
        }
        _mainWindow = null;
    }

    private void InitializeTrayIcon()
    {
        var className = "VKVideoDesktop_TrayWnd";
        _gchThis = GCHandle.Alloc(this);

        var wc = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = GetModuleHandle("VKVideoDesktop.App"),
            lpszClassName = className
        };

        RegisterClass(ref wc);

        _windowHandle = CreateWindowEx(
            0, className, "VKVideoDesktop_Tray",
            0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

        var iconHandle = LoadImage(
            GetModuleHandle("VKVideoDesktop.App"),
            "IDR_MAINFRAME",
            1,
            16, 16,
            0x00000010);

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = LoadIcon(IntPtr.Zero, (IntPtr)32512);
        }

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _windowHandle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = iconHandle,
            szTip = "VK Video Desktop"
        };

        Shell_NotifyIcon(NIM_ADD, ref nid);
        _trayIconHandle = nid.hIcon;
    }

    private void RemoveTrayIcon()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _windowHandle,
                uID = 1
            };

            Shell_NotifyIcon(NIM_DELETE, ref nid);

            DestroyWindow(_windowHandle);
            _windowHandle = IntPtr.Zero;

            if (_gchThis.IsAllocated)
                _gchThis.Free();
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);

            if (mouseMsg == WM_LBUTTONDBLCLK)
            {
                ShowMainWindow();
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                ShowTrayContextMenu(hWnd);
            }

            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowTrayContextMenu(IntPtr hWnd)
    {
        var hMenu = CreatePopupMenu();
        var localization = Services.GetRequiredService<LocalizationService>();
        AppendMenu(hMenu, 0, ID_TRAY_SHOW, localization["TrayShow"]);
        AppendMenu(hMenu, 0x0800, 0, string.Empty);
        AppendMenu(hMenu, 0, ID_TRAY_EXIT, localization["TrayExit"]);

        SetForegroundWindow(hWnd);

        POINT pt;
        GetCursorPos(out pt);

        int cmd = TrackPopupMenu(hMenu, 0x0001 | 0x0100, pt.X, pt.Y, 0, hWnd, IntPtr.Zero);
        DestroyMenu(hMenu);

        if (cmd == ID_TRAY_SHOW)
        {
            ShowMainWindow();
        }
        else if (cmd == ID_TRAY_EXIT)
        {
            ExitApp();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = Services.GetRequiredService<MainWindow>();
            _mainWindow.Closed += OnMainWindowClosed;
        }

        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _isClosing = true;
        RemoveTrayIcon();
        _mainWindow?.Close();
        Exit();
    }

    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string? lpszClassName;
    }

    [DllImport("user32.dll")]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
