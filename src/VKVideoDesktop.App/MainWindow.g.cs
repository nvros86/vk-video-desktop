using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using WinRT;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow
{
    internal NavigationView NavView = null!;
    internal Frame ContentFrame = null!;
    internal Grid MiniPlayerBar = null!;
    internal TextBlock MiniPlayerTitle = null!;
    internal Image MiniPlayerThumb = null!;
    internal Grid DownloadsPanel = null!;
    internal AutoSuggestBox SearchBox = null!;
    internal Grid ErrorBanner = null!;
    internal TextBlock ErrorBannerText = null!;
    internal Border TopBarDownloadBadge = null!;
    internal TextBlock TopBarDownloadCount = null!;
    internal StackPanel OfflineBanner = null!;
    internal Border DownloadBadge = null!;
    internal TextBlock DownloadBadgeCount = null!;
    internal void InitializeComponent()
    {
        try
        {
            Microsoft.UI.Xaml.Application.LoadComponent(this, new Uri("ms-appx:///MainWindow.xaml"),
                Microsoft.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
            return;
        }
        catch { }

        try
        {
            var xamlPath = Path.Combine(AppContext.BaseDirectory, "MainWindow.xaml");
            if (!File.Exists(xamlPath)) return;
            var xaml = File.ReadAllText(xamlPath);
            var contentXaml = ExtractInnerContent(xaml);
            var grid = (FrameworkElement)XamlReader.Load(contentXaml);
            Content = grid;
            NavView = (NavigationView)grid.FindName("NavView");
            ContentFrame = (Frame)grid.FindName("ContentFrame");
            MiniPlayerBar = (Grid)grid.FindName("MiniPlayerBar");
            MiniPlayerTitle = (TextBlock)grid.FindName("MiniPlayerTitle");
            MiniPlayerThumb = (Image)grid.FindName("MiniPlayerThumb");
            DownloadsPanel = (Grid)grid.FindName("DownloadsPanel");
            SearchBox = (AutoSuggestBox)grid.FindName("SearchBox");
            ErrorBanner = (Grid)grid.FindName("ErrorBanner");
            ErrorBannerText = (TextBlock)grid.FindName("ErrorBannerText");
            TopBarDownloadBadge = (Border)grid.FindName("TopBarDownloadBadge");
            TopBarDownloadCount = (TextBlock)grid.FindName("TopBarDownloadCount");
            OfflineBanner = (StackPanel)grid.FindName("OfflineBanner");
            DownloadBadge = (Border)grid.FindName("DownloadBadge");
            DownloadBadgeCount = (TextBlock)grid.FindName("DownloadBadgeCount");
        }
        catch { }
    }

    private static string ExtractInnerContent(string xaml)
    {
        var openTagEnd = xaml.IndexOf('>');
        if (openTagEnd < 0) return xaml;
        var closeTagStart = xaml.LastIndexOf("</Window");
        if (closeTagStart < 0) closeTagStart = xaml.LastIndexOf("</");
        if (closeTagStart < 0) return xaml.Substring(openTagEnd + 1);
        return xaml.Substring(openTagEnd + 1, closeTagStart - openTagEnd - 1).Trim();
    }
}
