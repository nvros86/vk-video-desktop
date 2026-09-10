using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Data.Database;
using VKVideoDesktop.Infrastructure.Cache;
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
    private IntPtr _oldWndProc;
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
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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
        InitializeComponent();

        _wndProcDelegate = WndProc;

        var resourcesPath = AppContext.BaseDirectory;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddLogging();

                services.AddSingleton(new LocalizationService(resourcesPath));

                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAuthenticationService, VkAuthenticationService>();
                services.AddSingleton<IVideoProvider, VkVideoProvider>();
                services.AddSingleton<IDownloadEngine, DownloadEngine>();
                services.AddSingleton<IDownloadRepository, DownloadDatabase>();
                services.AddSingleton<IDownloadSourceResolver, VkVideoSourceResolver>();
                services.AddSingleton<IDownloadManager, DownloadManager>();
                services.AddSingleton<IThumbnailCache, ThumbnailCache>();
                services.AddSingleton<IHistoryService, HistoryDatabase>();
                services.AddSingleton<IFavoritesService, FavoritesDatabase>();
                services.AddSingleton<IPlaylistService, PlaylistDatabase>();
                services.AddSingleton<PlaybackService>();
                services.AddSingleton<ErrorHandlerService>();
                services.AddSingleton<DeepLinkService>();

                services.AddTransient<SearchService>();
                services.AddTransient<VideoService>();
                services.AddTransient<DownloadService>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        Services = _host.Services;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
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
            hInstance = GetModuleHandle(null),
            lpszClassName = className
        };

        RegisterClass(ref wc);

        _windowHandle = CreateWindowEx(
            0, className, "VKVideoDesktop_Tray",
            0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, wc.hInstance, IntPtr.Zero);

        var iconHandle = LoadImage(
            GetModuleHandle(null),
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

            if (_oldWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_windowHandle, -4, _oldWndProc);
            }

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
        public string lpszMenuName;
        public string lpszClassName;
    }

    [DllImport("user32.dll")]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
