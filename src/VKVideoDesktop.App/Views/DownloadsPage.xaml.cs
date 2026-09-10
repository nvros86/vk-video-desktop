using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; }

    public DownloadsPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<DownloadsViewModel>();
        DataContext = ViewModel;
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

    private async void OnClearCompletedClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearCompletedAsync();
    }

    private async void OnDownloadAllClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.RetryAllFailedAsync();
    }
}
