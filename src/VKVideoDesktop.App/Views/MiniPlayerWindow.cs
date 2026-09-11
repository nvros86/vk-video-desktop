using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using VKVideoDesktop.Application.Services;
using WinRT.Interop;

namespace VKVideoDesktop.App.Views;

public sealed class MiniPlayerWindow : Window
{
    private readonly PlaybackService _playbackService;
    private AppWindow _appWindow = null!;

    public MiniPlayerWindow()
    {
        _playbackService = App.GetService<PlaybackService>();

        Title = "VK Video Mini";

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        _appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 480, Height = 320 });

        var presenter = OverlappedPresenter.Create();
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = true;
        presenter.IsResizable = true;
        presenter.SetBorderAndTitleBar(true, true);
        _appWindow.SetPresenter(presenter);

        _appWindow.TitleBar.ExtendsContentIntoTitleBar = false;
        SystemBackdrop = null;

        Closed += OnClosed;
    }

    public void UpdateTitle(string videoTitle)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Title = videoTitle;
        });
    }

    private void OnClosed(object? sender, WindowEventArgs args)
    {
        _playbackService.ClearQueue();
    }
}
