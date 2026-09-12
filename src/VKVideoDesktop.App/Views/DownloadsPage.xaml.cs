using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.App.ViewModels;
using Windows.Storage;

namespace VKVideoDesktop.App.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; }
    private readonly ILogger<DownloadsPage> _logger;
    private readonly LocalizationService _localization;

    public DownloadsPage()
    {
        _logger = App.GetService<ILogger<DownloadsPage>>();
        _logger.LogInformation("[DownloadsPage] Constructor");
        InitializeComponent();
        ViewModel = App.GetService<DownloadsViewModel>();
        DataContext = ViewModel;
        _localization = App.GetService<LocalizationService>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            await ViewModel.LoadDownloadsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load downloads");
        }
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string downloadId)
        {
            _ = ViewModel.PauseAsync(downloadId);
        }
    }

    private void OnResumeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string downloadId)
        {
            _ = ViewModel.ResumeAsync(downloadId);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string downloadId)
        {
            _ = ViewModel.CancelAsync(downloadId);
        }
    }

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadItemViewModel dl)
        {
            var flyout = new MenuFlyout();
            var openFileItem = new MenuFlyoutItem();
            openFileItem.Text = _localization["DownloadsOpenFile"];
            openFileItem.Tag = dl;
            openFileItem.Click += OnOpenFileClick;
            flyout.Items.Add(openFileItem);

            var openFolderItem = new MenuFlyoutItem();
            openFolderItem.Text = _localization["DownloadsOpenFolder"];
            openFolderItem.Tag = dl;
            openFolderItem.Click += OnOpenFolderClick;
            flyout.Items.Add(openFolderItem);

            var copyItem = new MenuFlyoutItem();
            copyItem.Text = _localization["DownloadsCopyPath"];
            copyItem.Tag = dl;
            copyItem.Click += OnCopyPathClick;
            flyout.Items.Add(copyItem);

            flyout.Items.Add(new MenuFlyoutSeparator());

            var deleteItem = new MenuFlyoutItem();
            deleteItem.Text = _localization["DownloadsDeleteRecord"];
            deleteItem.Tag = dl;
            deleteItem.Click += OnDeleteRecordClick;
            flyout.Items.Add(deleteItem);

            flyout.ShowAt(button);
        }
    }

    private async void OnOpenFileClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is DownloadItemViewModel dl)
        {
            if (!string.IsNullOrEmpty(dl.DestinationPath) && System.IO.File.Exists(dl.DestinationPath))
            {
                var file = await StorageFile.GetFileFromPathAsync(dl.DestinationPath);
                await Windows.System.Launcher.LaunchFileAsync(file);
            }
        }
    }

    private async void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is DownloadItemViewModel dl)
        {
            if (!string.IsNullOrEmpty(dl.DestinationPath))
            {
                var folder = System.IO.Path.GetDirectoryName(dl.DestinationPath);
                if (folder != null)
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(folder));
            }
        }
    }

    private void OnCopyPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is DownloadItemViewModel dl)
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(dl.DestinationPath);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
        }
    }

    private async void OnDeleteRecordClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is DownloadItemViewModel dl)
        {
            await ViewModel.RemoveDownloadAsync(dl.DownloadId);
        }
    }

    private async void OnClearCompletedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearCompletedAsync();
    }

    private async void OnAddDownloadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Добавить загрузку",
            PrimaryButtonText = "Скачать",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var textBox = new TextBox { PlaceholderText = "Введите URL видео", Margin = new Thickness(0, 12, 0, 0) };
        dialog.Content = textBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            // TODO: Implement download from URL
        }
    }
}
