using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.Views;

public sealed partial class HistoryPage : Page
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryPage> _logger;
    private readonly LocalizationService _localization;

    public HistoryPage()
    {
        _logger = App.GetService<ILogger<HistoryPage>>();
        _logger.LogInformation("[HistoryPage] Constructor");
        InitializeComponent();
        _historyService = App.GetService<IHistoryService>();
        _localization = App.GetService<LocalizationService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var entries = await _historyService.GetAllAsync();
        HistoryListView.ItemsSource = entries;
        EmptyState.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HistoryEntry entry)
        {
            await _historyService.DeleteAsync(entry.Id);
            OnLoaded(this, new RoutedEventArgs());
        }
    }

    private async void OnClearHistoryClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Очистить историю",
            Content = "Вы уверены, что хотите очистить историю просмотров?",
            PrimaryButtonText = "Очистить",
            CloseButtonText = "Отмена",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _historyService.ClearAsync();
            OnLoaded(this, new RoutedEventArgs());
        }
    }
}
