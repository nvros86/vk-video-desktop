using System;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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

    private static Brush Mp => new SolidColorBrush(Colors.White);
    private static Brush Ms => new SolidColorBrush(Colors.Gray);
    private static Brush Ma => new SolidColorBrush(ColorHelper.FromArgb(255, 88, 166, 255));
    private static Brush Mt => new SolidColorBrush(Colors.Transparent);

    internal void InitializeComponent()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        Content = root;

        var topBar = BuildTopBar();
        Grid.SetRow(topBar, 0);
        root.Children.Add(topBar);

        var mainContent = BuildMainContent();
        Grid.SetRow(mainContent, 1);
        root.Children.Add(mainContent);

        MiniPlayerBar = BuildMiniPlayerBar();
        Grid.SetRow(MiniPlayerBar, 2);
        root.Children.Add(MiniPlayerBar);

        ErrorBanner = BuildErrorBanner();
        Grid.SetRow(ErrorBanner, 1);
        root.Children.Add(ErrorBanner);

        DownloadsPanel = BuildDownloadsPanel();
        Grid.SetRow(DownloadsPanel, 1);
        root.Children.Add(DownloadsPanel);
    }

    private Grid BuildTopBar()
    {
        var bar = new Grid();
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
        bar.Height = 52;
        bar.Padding = new Thickness(16, 10, 16, 10);

        var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new FontIcon { Glyph = "\uE72B", FontSize = 20, Foreground = Ma });
        titleStack.Children.Add(new TextBlock
        {
            Text = "VK Video Desktop",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = Mp,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 24, 0)
        });
        Grid.SetColumn(titleStack, 0);
        bar.Children.Add(titleStack);

        SearchBox = new AutoSuggestBox
        {
            PlaceholderText = "\u041f\u043e\u0438\u0441\u043a \u0432\u0438\u0434\u0435\u043e...",
            QueryIcon = new SymbolIcon(Symbol.Find),
            Width = 500,
            MaxWidth = 600,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        SearchBox.KeyDown += OnSearchBoxKeyDown;
        Grid.SetColumn(SearchBox, 1);
        bar.Children.Add(SearchBox);

        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

        var dlButton = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(dlButton, "\u0417\u0430\u0433\u0440\u0443\u0437\u043a\u0438");
        dlButton.Click += OnDownloadsClick;
        dlButton.Content = new FontIcon { Glyph = "\uE896", Foreground = Ms };
        rightPanel.Children.Add(dlButton);

        var settingsButton = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(settingsButton, "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438");
        settingsButton.Click += OnSettingsClick;
        settingsButton.Content = new FontIcon { Glyph = "\uE713", Foreground = Ms };
        rightPanel.Children.Add(settingsButton);

        Grid.SetColumn(rightPanel, 2);
        bar.Children.Add(rightPanel);

        return bar;
    }

    private Grid BuildMainContent()
    {
        var main = new Grid();
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        NavView = new NavigationView
        {
            Width = 220,
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            IsSettingsVisible = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsTitleBarAutoPaddingEnabled = false,
            OpenPaneLength = 220,
            CompactPaneLength = 48
        };
        NavView.SelectionChanged += OnNavSelectionChanged;
        NavView.Loaded += OnNavViewLoaded;

        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u0413\u043b\u0430\u0432\u043d\u0430\u044f", Tag = "Home", Icon = new FontIcon { Glyph = "\uE80F" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u041f\u043e\u0438\u0441\u043a", Tag = "Search", Icon = new FontIcon { Glyph = "\uE721" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u041c\u043e\u0438 \u0432\u0438\u0434\u0435\u043e", Tag = "Profile", Icon = new FontIcon { Glyph = "\uE8B4" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u041f\u043e\u0434\u043f\u0438\u0441\u043a\u0438", Tag = "Favorites", Icon = new FontIcon { Glyph = "\uE734" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u0418\u0437\u0431\u0440\u0430\u043d\u043d\u043e\u0435", Tag = "Favorites2", Icon = new FontIcon { Glyph = "\uE734" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u041f\u043b\u0435\u0439\u043b\u0438\u0441\u0442\u044b", Tag = "Playlists", Icon = new FontIcon { Glyph = "\uE8B4" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u0418\u0441\u0442\u043e\u0440\u0438\u044f", Tag = "History", Icon = new FontIcon { Glyph = "\uE81C" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u0417\u0430\u0433\u0440\u0443\u0437\u043a\u0438", Tag = "Downloads", Icon = new FontIcon { Glyph = "\uE896" } });
        NavView.MenuItems.Add(new NavigationViewItem { Content = "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438", Tag = "Settings", Icon = new FontIcon { Glyph = "\uE713" } });

        var navContent = new Grid();
        navContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        navContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        OfflineBanner = new StackPanel { Visibility = Visibility.Collapsed, Background = new SolidColorBrush(Colors.OrangeRed), Padding = new Thickness(12, 6, 12, 6) };
        OfflineBanner.Children.Add(new TextBlock { Text = "\u041d\u0435\u0442 \u043f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u044f.", FontSize = 12 });
        Grid.SetRow(OfflineBanner, 0);
        navContent.Children.Add(OfflineBanner);

        ContentFrame = new Frame();
        Grid.SetRow(ContentFrame, 1);
        navContent.Children.Add(ContentFrame);

        NavView.Content = navContent;
        Grid.SetColumn(NavView, 0);
        main.Children.Add(NavView);

        return main;
    }

    private Grid BuildMiniPlayerBar()
    {
        var bar = new Grid { Height = 52, Padding = new Thickness(16, 0, 16, 0), Visibility = Visibility.Collapsed };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });

        MiniPlayerThumb = new Image { Width = 80, Height = 46, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 12, 0) };
        Grid.SetColumn(MiniPlayerThumb, 0);
        bar.Children.Add(MiniPlayerThumb);

        MiniPlayerTitle = new TextBlock { TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontSize = 13, Foreground = Mp };
        Grid.SetColumn(MiniPlayerTitle, 1);
        bar.Children.Add(MiniPlayerTitle);

        var miniButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };

        var playBtn = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(playBtn, "\u0412\u043e\u0441\u043f\u0440\u043e\u0438\u0437\u0432\u0435\u0441\u0442\u0438/\u041f\u0430\u0437\u0443\u0437\u0430");
        playBtn.Click += OnMiniPlayerPlayClick;
        playBtn.Content = new FontIcon { Glyph = "\uE768", FontSize = 14, Foreground = Mp };
        miniButtons.Children.Add(playBtn);

        var prevBtn = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(prevBtn, "\u041f\u0440\u0435\u0434\u044b\u0434\u0443\u0449\u0435\u0435");
        prevBtn.Click += OnMiniPlayerPrevClick;
        prevBtn.Content = new FontIcon { Glyph = "\uE892", FontSize = 14, Foreground = Ms };
        miniButtons.Children.Add(prevBtn);

        var nextBtn = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(nextBtn, "\u0421\u043b\u0435\u0434\u0443\u044e\u0449\u0435\u0435");
        nextBtn.Click += OnMiniPlayerNextClick;
        nextBtn.Content = new FontIcon { Glyph = "\uE893", FontSize = 14, Foreground = Ms };
        miniButtons.Children.Add(nextBtn);

        var fsBtn = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(fsBtn, "\u041f\u043e\u043b\u043d\u044b\u0439 \u044d\u043a\u0440\u0430\u043d");
        fsBtn.Click += OnMiniPlayerFullscreenClick;
        fsBtn.Content = new FontIcon { Glyph = "\uE740", FontSize = 14, Foreground = Ms };
        miniButtons.Children.Add(fsBtn);

        var closeBtn = new Button { Background = Mt, BorderThickness = new Thickness(0) };
        ToolTipService.SetToolTip(closeBtn, "\u0417\u0430\u043a\u0440\u044b\u0442\u044c");
        closeBtn.Click += OnMiniPlayerCloseClick;
        closeBtn.Content = new FontIcon { Glyph = "\uE8BB", FontSize = 14, Foreground = Ms };
        miniButtons.Children.Add(closeBtn);

        Grid.SetColumn(miniButtons, 2);
        bar.Children.Add(miniButtons);

        return bar;
    }

    private Grid BuildErrorBanner()
    {
        var banner = new Grid { Visibility = Visibility.Collapsed, Padding = new Thickness(12, 8, 12, 8), HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
        banner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        banner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
        banner.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 53, 69));

        ErrorBannerText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Colors.White) };
        Grid.SetColumn(ErrorBannerText, 0);
        banner.Children.Add(ErrorBannerText);

        var closeBtn = new Button { Background = Mt, Foreground = new SolidColorBrush(Colors.White) };
        closeBtn.Content = "\uE8BB";
        closeBtn.FontFamily = new FontFamily("Segoe MDL2 Assets");
        closeBtn.Click += OnCloseErrorBanner;
        Grid.SetColumn(closeBtn, 1);
        banner.Children.Add(closeBtn);

        return banner;
    }

    private Grid BuildDownloadsPanel()
    {
        var panel = new Grid
        {
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 360,
            Height = 400,
            Margin = new Thickness(0, 0, 12, 12)
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Padding = new Thickness(12, 8, 12, 8) };
        header.Children.Add(new TextBlock { Text = "\u0417\u0430\u0433\u0440\u0443\u0437\u043a\u0438", VerticalAlignment = VerticalAlignment.Center, Foreground = Mp });
        var closeBtn = new Button { HorizontalAlignment = HorizontalAlignment.Right, Background = Mt };
        closeBtn.Content = "\uE8BB";
        closeBtn.FontFamily = new FontFamily("Segoe MDL2 Assets");
        closeBtn.Click += OnCloseDownloadsPanel;
        header.Children.Add(closeBtn);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        return panel;
    }
}
