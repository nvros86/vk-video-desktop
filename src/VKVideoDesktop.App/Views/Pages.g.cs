using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace VKVideoDesktop.App.Views;

public sealed partial class HomePage
{
    internal StackPanel EmptyState = null!;
    internal void InitializeComponent() { }
}

public sealed partial class SearchPage
{
    internal ComboBox SortComboBox = null!;
    internal Button LoadMoreButton = null!;
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
    internal Slider VolumeSlider = null!;
    internal ComboBox SpeedComboBox = null!;
    internal CheckBox UseProxyCheckBox = null!;
    internal TextBox ProxyAddressTextBox = null!;
    internal void InitializeComponent() { }
}

public sealed partial class ProfilePage
{
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
    internal void InitializeComponent()
    {
        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 24, 24, 32));

        var outerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 500 };
        var stack = new StackPanel { Spacing = 24, Margin = new Thickness(24) };

        stack.Children.Add(new TextBlock { Text = "\u0412\u0445\u043e\u0434 \u0432 VK Video", FontSize = 24, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) });

        Step1Text = new TextBlock { Text = "1. \u041d\u0430\u0436\u043c\u0438\u0442\u0435 \u043a\u043d\u043e\u043f\u043a\u0443 \u043d\u0438\u0436\u0435, \u0447\u0442\u043e\u0431\u044b \u043e\u0442\u043a\u0440\u044b\u0442\u044c \u0441\u0442\u0440\u0430\u043d\u0438\u0446\u0443 \u0430\u0432\u0442\u043e\u0440\u0438\u0437\u0430\u0446\u0438\u0438 VK", TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) };
        stack.Children.Add(Step1Text);

        LoginButton = new Button { Content = "\u0412\u043e\u0439\u0442\u0438 \u0447\u0435\u0440\u0435\u0437 VK", HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(32, 12, 32, 12) };
        LoginButton.Click += OnLoginClick;
        stack.Children.Add(LoginButton);

        Step2Text = new TextBlock { Text = "2. \u041f\u043e\u0441\u043b\u0435 \u0430\u0432\u0442\u043e\u0440\u0438\u0437\u0430\u0446\u0438\u0438 \u0441\u043a\u043e\u043f\u0438\u0440\u0443\u0439\u0442\u0435 URL \u0438\u0437 \u0430\u0434\u0440\u0435\u0441\u043d\u043e\u0439 \u0441\u0442\u0440\u043e\u043a\u0438 \u0431\u0440\u0430\u0443\u0437\u0435\u0440\u0430 \u0438 \u0432\u0441\u0442\u0430\u0432\u044c\u0442\u0435 \u0435\u0433\u043e \u0441\u044e\u0434\u0430", TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray), Visibility = Visibility.Collapsed };
        stack.Children.Add(Step2Text);

        TokenInput = new TextBox { PlaceholderText = "\u0412\u0441\u0442\u0430\u0432\u044c\u0442\u0435 URL \u0438\u043b\u0438 \u0442\u043e\u043a\u0435\u043d \u0441\u044e\u0434\u0430...", Visibility = Visibility.Collapsed };
        stack.Children.Add(TokenInput);

        SubmitTokenButton = new Button { Content = "\u0412\u043e\u0439\u0442\u0438", HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(32, 12, 32, 12), Visibility = Visibility.Collapsed };
        SubmitTokenButton.Click += OnSubmitTokenClick;
        stack.Children.Add(SubmitTokenButton);

        ErrorText = new TextBlock { Text = "", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red), HorizontalAlignment = HorizontalAlignment.Center, Visibility = Visibility.Collapsed };
        stack.Children.Add(ErrorText);

        LoadingText = new TextBlock { Text = "\u041f\u0440\u043e\u0432\u0435\u0440\u043a\u0430 \u0442\u043e\u043a\u0435\u043d\u0430...", HorizontalAlignment = HorizontalAlignment.Center, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray), Visibility = Visibility.Collapsed };
        stack.Children.Add(LoadingText);

        outerGrid.Children.Add(stack);
        Content = outerGrid;
    }
}

public sealed partial class ChannelPage
{
    internal Image BannerImage = null!;
    internal ImageBrush AvatarBrush = null!;
    internal TextBlock ChannelNameText = null!;
    internal TextBlock ChannelUsernameText = null!;
    internal TextBlock SubscriberCountText = null!;
    internal TextBlock DescriptionText = null!;
    internal ItemsControl ChannelVideos = null!;
    internal void InitializeComponent() { }
}

public sealed partial class WebViewVideoPage
{
    internal Button BackButton = null!;
    internal TextBlock TitleText = null!;
    internal Button OpenInBrowserButton = null!;
    internal Microsoft.UI.Xaml.Controls.WebView2 WebView = null!;
    internal void InitializeComponent() { }
}
