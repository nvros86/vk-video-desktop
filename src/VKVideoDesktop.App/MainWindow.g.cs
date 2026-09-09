using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VKVideoDesktop.App;

public sealed partial class MainWindow
{
    internal void InitializeComponent() { }

    internal Frame ContentFrame = null!;
    internal NavigationView NavView = null!;
    internal AutoSuggestBox SearchBox = null!;
    internal Grid DownloadsPanel = null!;
    internal Grid MiniPlayerBar = null!;
}
