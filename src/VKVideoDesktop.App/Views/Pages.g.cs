using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace VKVideoDesktop.App.Views;

public sealed partial class HomePage
{
    internal void InitializeComponent() { }
}

public sealed partial class SearchPage
{
    internal void InitializeComponent() { }
}

public sealed partial class VideoPage
{
    internal MediaPlayerElement MediaPlayerElement = null!;
    internal Button PlayButton = null!;
    internal FontIcon PlayIcon = null!;
    internal ProgressBar PositionBar = null!;
    internal TextBlock TimeText = null!;
    internal Image ThumbnailImage = null!;
    internal Button ControlPlayButton = null!;
    internal Button VolumeButton = null!;
    internal Button SpeedButton = null!;
    internal Button QualityButton = null!;
    internal Grid PlayerContainer = null!;
    internal void InitializeComponent() { }
}

public sealed partial class FavoritesPage
{
    internal StackPanel EmptyState = null!;
    internal ItemsControl FavoritesList = null!;
    internal void InitializeComponent() { }
}

public sealed partial class PlaylistsPage
{
    internal StackPanel EmptyState = null!;
    internal ItemsControl PlaylistsList = null!;
    internal void InitializeComponent() { }
}

public sealed partial class HistoryPage
{
    internal StackPanel EmptyState = null!;
    internal ListView HistoryList = null!;
    internal void InitializeComponent() { }
}

public sealed partial class DownloadsPage
{
    internal void InitializeComponent() { }
}

public sealed partial class SettingsPage
{
    internal ComboBox LanguageCombo = null!;
    internal ComboBox ThemeCombo = null!;
    internal ToggleSwitch StartWithWindowsToggle = null!;
    internal ToggleSwitch MinimizeToTrayToggle = null!;
    internal ToggleSwitch NotificationsToggle = null!;
    internal ComboBox QualityCombo = null!;
    internal ToggleSwitch AutoplayToggle = null!;
    internal TextBlock DownloadFolderText = null!;
    internal ComboBox MaxDownloadsCombo = null!;
    internal ComboBox SpeedLimitCombo = null!;
    internal NumberBox RetryCountBox = null!;
    internal ToggleSwitch AutoResumeToggle = null!;
    internal ToggleSwitch DeletePartToggle = null!;
    internal ComboBox QualityBehaviorCombo = null!;
    internal void InitializeComponent() { }
}

public sealed partial class LoginPage
{
    internal TextBlock Step1Text = null!;
    internal Button LoginButton = null!;
    internal TextBlock Step2Text = null!;
    internal TextBox TokenInput = null!;
    internal Button SubmitTokenButton = null!;
    internal TextBlock ErrorText = null!;
    internal TextBlock LoadingText = null!;
    internal void InitializeComponent() { }
}
