using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly ISettingsService _settingsService;
    private bool _isLoading;

    public SettingsPage()
    {
        InitializeComponent();
        _settingsService = App.GetService<ISettingsService>();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        LoadSettings();
        _isLoading = false;
    }

    private void LoadSettings()
    {
        var s = _settingsService.Settings;
        LanguageCombo.SelectedIndex = s.Language == "ru" ? 0 : 1;
        ThemeCombo.SelectedIndex = (int)s.Theme;
        StartWithWindowsToggle.IsOn = s.StartWithWindows;
        MinimizeToTrayToggle.IsOn = s.MinimizeToTray;
        NotificationsToggle.IsOn = s.EnableNotifications;
        QualityCombo.SelectedIndex = s.DefaultQuality switch
        {
            "240p" => 0,
            "360p" => 1,
            "480p" => 2,
            "720p" => 3,
            "1080p" => 4,
            "1440p" => 5,
            "2160p" => 6,
            _ => 4
        };
        AutoplayToggle.IsOn = s.Autoplay;
        VolumeSlider.Value = s.DefaultVolume * 100;
        var speedIndex = s.DefaultPlaybackSpeed switch
        {
            0.5 => 0,
            0.75 => 1,
            1.0 => 2,
            1.25 => 3,
            1.5 => 4,
            2.0 => 5,
            _ => 2
        };
        SpeedComboBox.SelectedIndex = speedIndex;
        DownloadFolderText.Text = s.DownloadFolder;
        MaxDownloadsCombo.SelectedIndex = s.MaxConcurrentDownloads - 1;
        SpeedLimitCombo.SelectedIndex = s.SpeedLimit switch
        {
            0 => 0,
            512 * 1024 => 1,
            1 * 1024 * 1024 => 2,
            2 * 1024 * 1024 => 3,
            5 * 1024 * 1024 => 4,
            10 * 1024 * 1024 => 5,
            _ => 0
        };
        RetryCountBox.Value = s.RetryCount;
        AutoResumeToggle.IsOn = s.AutoResumeAfterStartup;
        DeletePartToggle.IsOn = s.DeletePartOnCancel;
        QualityBehaviorCombo.SelectedIndex = (int)s.DownloadQualityBehavior;
        UseProxyCheckBox.IsChecked = s.UseProxy;
        ProxyAddressTextBox.Text = s.ProxyAddress;
    }

    private async void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        await SaveSettings();
    }

    private async void OnSettingChanged_Combo(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        await SaveSettings();
    }

    private async void OnSettingChanged_Toggle(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        await SaveSettings();
    }

    private async void OnRetryCountChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading) return;
        await SaveSettings();
    }

    private async void OnVolumeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isLoading) return;
        _settingsService.Settings.DefaultVolume = e.NewValue / 100.0;
        await _settingsService.SaveAsync();
    }

    private async void OnSpeedChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        if (SpeedComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (double.TryParse(tag, System.Globalization.CultureInfo.InvariantCulture, out var speed))
            {
                _settingsService.Settings.DefaultPlaybackSpeed = speed;
                await _settingsService.SaveAsync();
            }
        }
    }

    private async void OnProxyChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _settingsService.Settings.UseProxy = UseProxyCheckBox.IsChecked == true;
        await _settingsService.SaveAsync();
    }

    private async void OnProxyAddressChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _settingsService.Settings.ProxyAddress = ProxyAddressTextBox.Text;
        await _settingsService.SaveAsync();
    }

    private async Task SaveSettings()
    {
        var s = _settingsService.Settings;
        s.Language = LanguageCombo.SelectedIndex == 0 ? "ru" : "en";
        s.Theme = (AppTheme)ThemeCombo.SelectedIndex;
        s.StartWithWindows = StartWithWindowsToggle.IsOn;
        s.MinimizeToTray = MinimizeToTrayToggle.IsOn;
        s.EnableNotifications = NotificationsToggle.IsOn;
        s.DefaultQuality = QualityCombo.SelectedIndex switch
        {
            0 => "240p",
            1 => "360p",
            2 => "480p",
            3 => "720p",
            4 => "1080p",
            5 => "1440p",
            6 => "2160p",
            _ => "1080p"
        };
        s.Autoplay = AutoplayToggle.IsOn;
        s.MaxConcurrentDownloads = MaxDownloadsCombo.SelectedIndex + 1;
        s.SpeedLimit = SpeedLimitCombo.SelectedIndex switch
        {
            1 => 512 * 1024,
            2 => 1 * 1024 * 1024,
            3 => 2 * 1024 * 1024,
            4 => 5 * 1024 * 1024,
            5 => 10 * 1024 * 1024,
            _ => 0
        };
        s.RetryCount = (int)RetryCountBox.Value;
        s.AutoResumeAfterStartup = AutoResumeToggle.IsOn;
        s.DeletePartOnCancel = DeletePartToggle.IsOn;
        s.DownloadQualityBehavior = (DownloadQualityBehavior)QualityBehaviorCombo.SelectedIndex;
        await _settingsService.SaveAsync();
    }

    private async void OnClearCacheClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить кэш",
            Content = "Вы уверены, что хотите очистить кэш изображений?",
            PrimaryButtonText = "Очистить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var cache = App.GetService<IThumbnailCache>();
            await cache.ClearCacheAsync();
        }
    }

    private async void OnChangeFolderClick(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.GetService<MainWindow>());
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _settingsService.Settings.DownloadFolder = folder.Path;
            DownloadFolderText.Text = folder.Path;
            await _settingsService.SaveAsync();
        }
    }
}
