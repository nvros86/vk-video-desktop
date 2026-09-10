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
        DebugLog("App constructor - InitializeComponent...");
        InitializeComponent();
        DebugLog("App constructor - done, starting DI...");

        _wndProcDelegate = WndProc;

        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VKVideoDesktop", "log");
        System.IO.Directory.CreateDirectory(logDir);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                WriteCrashLog(logDir, "UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog(logDir, "UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        var resourcesPath = AppContext.BaseDirectory;

        DebugLog("Building host...");
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop", "log", "app-.log");

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

                services.AddSingleton(new LocalizationService(resourcesPath));

                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAuthenticationService, VkAuthenticationService>();
                services.AddSingleton<IVideoProvider, VkVideoProvider>();
                services.AddSingleton<IDownloadEngine, DownloadEngine>();
                services.AddSingleton<IDownloadRepository, DownloadDatabase>();
                services.AddSingleton<IDownloadSourceResolver, VkVideoSourceResolver>();
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
            })
            .Build();

        Services = _host.Services;
        DebugLog("App constructor - host built, Services assigned");
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        DebugLog("OnLaunched - entry point reached");
        try
        {
            DebugLog("OnLaunched - loading styles...");
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var colorsPath = System.IO.Path.Combine(baseDir, "Styles", "Colors.xaml");
                var stylesPath = System.IO.Path.Combine(baseDir, "Styles", "Styles.xaml");
                DebugLog($"BaseDir: {baseDir}, Colors exists: {System.IO.File.Exists(colorsPath)}, Styles exists: {System.IO.File.Exists(stylesPath)}");

                if (System.IO.File.Exists(colorsPath))
                {
                    var colorsXaml = System.IO.File.ReadAllText(colorsPath);
                    var colorsDict = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(colorsXaml);
                    Resources.MergedDictionaries.Add(colorsDict);
                    DebugLog("Colors.xaml loaded");
                }

                if (System.IO.File.Exists(stylesPath))
                {
                    var stylesXaml = System.IO.File.ReadAllText(stylesPath);
                    var stylesDict = (ResourceDictionary)Microsoft.UI.Xaml.Markup.XamlReader.Load(stylesXaml);
                    Resources.MergedDictionaries.Add(stylesDict);
                    DebugLog("Styles.xaml loaded");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to load styles: {ex}");
            }

            await _host.StartAsync();

            var settingsService = Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            _mainWindow = Services.GetRequiredService<MainWindow>();
            _mainWindow.Activate();
            _mainWindow.Closed += OnMainWindowClosed;

            InitializeTrayIcon();

            if (args.Arguments?.StartsWith("vkvideo://") == true || args.Arguments?.StartsWith("vkvideo:") == true)
            {
                var deepLinkService = Services.GetRequiredService<DeepLinkService>();
                var result = deepLinkService.ProcessString(args.Arguments);
                if (result != null)
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
            DebugLog($"OnLaunched FAILED: {ex}");
        }
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

    private static void DebugLog(string message)
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VKVideoDesktop", "log", "debug.log");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [App] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private static void WriteCrashLog(string logDir, string type, Exception ex)
    {
        try
        {
            var crashPath = System.IO.Path.Combine(logDir, $"crash-{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== VK Video Desktop Crash Report ===");
            sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Тип: {type}");
            sb.AppendLine($"Версия ОС: {Environment.OSVersion}");
            sb.AppendLine($"64-bit: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"CLR: {Environment.Version}");
            sb.AppendLine();
            sb.AppendLine($"Исключение: {ex.GetType().FullName}");
            sb.AppendLine($"Сообщение: {ex.Message}");
            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Внутреннее исключение: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"Сообщение: {ex.InnerException.Message}");
            }
            sb.AppendLine();
            sb.AppendLine($"Стек вызовов:");
            sb.AppendLine(ex.StackTrace);
            if (ex.Data.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Дополнительные данные:");
                foreach (System.Collections.DictionaryEntry entry in ex.Data)
                    sb.AppendLine($"  {entry.Key}: {entry.Value}");
            }
            System.IO.File.WriteAllText(crashPath, sb.ToString());
        }
        catch
        {
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
        AppendMenu(hMenu, 0, ID_TRAY_SHOW, "Показать");
        AppendMenu(hMenu, 0x0800, 0, null);
        AppendMenu(hMenu, 0, ID_TRAY_EXIT, "Выход");

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

        var handle = _mainWindow.AppWindow.Id.Value;
        var interop = Marshal.GetIUnknownForObject(_mainWindow);
        _ = interop;
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
