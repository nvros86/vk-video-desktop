using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class SearchPage : Page
{
    private MainViewModel ViewModel { get; }
    private readonly DispatcherTimer _debounceTimer;

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounceTimer.Tick += OnDebounceTick;

        EmptyState.Visibility = Visibility.Visible;
        ResultsList.Visibility = Visibility.Collapsed;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string query && !string.IsNullOrEmpty(query))
        {
            SearchBox.Text = query;
            _ = ViewModel.SearchAsync(query);
        }
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void OnDebounceTick(object sender, object e)
    {
        _debounceTimer.Stop();

        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            ResultsList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        LoadingRing.IsActive = true;
        EmptyState.Visibility = Visibility.Collapsed;

        try
        {
            await ViewModel.SearchAsync(query);
            ResultsList.Visibility = ViewModel.SearchResults.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (ViewModel.SearchResults.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void OnSearchSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _debounceTimer.Stop();
        var query = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            _ = ViewModel.SearchAsync(query);
        }
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            _ = ViewModel.SearchAsync(query);
        }
    }

    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            _ = ViewModel.SearchAsync(query);
        }
    }

    private void OnVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }
}
