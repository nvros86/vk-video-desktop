using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class SettingsPage : Page
{
    private readonly ISettingsService _settingsService;
    private readonly LocalizationService _localization;
    private readonly ILogger<SettingsPage> _logger;
    private bool _isLoading;

    public SettingsPage()
    {
        _isLoading = true;
        _logger = App.GetService<ILogger<SettingsPage>>();
        _logger.LogInformation("[SettingsPage] Constructor");
        InitializeComponent();
        _settingsService = App.GetService<ISettingsService>();
        _localization = App.GetService<LocalizationService>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isLoading = true;
        _isLoading = false;
    }

    private void OnSettingsTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        var selectedTag = (SettingsTabs.SelectedItem as ListViewItem)?.Tag?.ToString();
        
        GeneralSettings.Visibility = selectedTag == "General" ? Visibility.Visible : Visibility.Collapsed;
        AppearanceSettings.Visibility = selectedTag == "Appearance" ? Visibility.Visible : Visibility.Collapsed;
        PlaybackSettings.Visibility = selectedTag == "Playback" ? Visibility.Visible : Visibility.Collapsed;
        DownloadSettings.Visibility = selectedTag == "Downloads" ? Visibility.Visible : Visibility.Collapsed;
        AboutSettings.Visibility = selectedTag == "About" ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnChangeDownloadFolderClick(object sender, RoutedEventArgs e)
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
            await _settingsService.SaveAsync();
        }
    }

    private async void OnClearCacheClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить кэш",
            Content = "Вы уверены, что хотите очистить кэш?",
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
}
