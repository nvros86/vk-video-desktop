using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow
{
    internal NavigationView NavView = null!;
    internal Frame ContentFrame = null!;
    internal Grid MiniPlayerBar = null!;
    internal TextBlock MiniPlayerTitle = null!;
    internal Grid DownloadsPanel = null!;
    internal AutoSuggestBox SearchBox = null!;
    internal Grid ErrorBanner = null!;
    internal TextBlock ErrorBannerText = null!;
    internal void InitializeComponent() { }
}
