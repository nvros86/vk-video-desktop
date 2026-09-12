using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Input;
using VKVideoDesktop.App.ViewModels;

namespace VKVideoDesktop.App.Views;

public sealed partial class SearchPage : Page
{
    private MainViewModel ViewModel { get; }
    private readonly DispatcherTimer _debounceTimer;
    private readonly ILogger<SearchPage> _logger;
    private string? _lastQuery;

    public SearchPage()
    {
        _logger = App.GetService<ILogger<SearchPage>>();
        _logger.LogInformation("[SearchPage] Constructor");
        InitializeComponent();
        ViewModel = App.GetService<MainViewModel>();

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounceTimer.Tick += OnDebounceTick;

        EmptyState.Visibility = Visibility.Visible;
        ResultsItemsControl.Visibility = Visibility.Collapsed;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _logger.LogInformation("[SearchPage] OnNavigatedTo");
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

    private async void OnDebounceTick(object? sender, object e)
    {
        _debounceTimer.Stop();

        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            ResultsItemsControl.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            LoadMoreButton.Visibility = Visibility.Collapsed;
            return;
        }

        _lastQuery = query;
        LoadingRing.IsActive = true;
        EmptyState.Visibility = Visibility.Collapsed;
        LoadMoreButton.Visibility = Visibility.Collapsed;

        try
        {
            await ViewModel.SearchAsync(query);
            ResultsItemsControl.Visibility = ViewModel.SearchResults.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (ViewModel.SearchResults.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
            }
            else if (ViewModel.SearchResults.Count >= 20)
            {
                LoadMoreButton.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            OnSearchQuerySubmitted(SearchBox, null);
        }
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _debounceTimer.Stop();
        var query = SearchBox.Text?.Trim();
        if (!string.IsNullOrEmpty(query))
        {
            _lastQuery = query;
            _ = ViewModel.SearchAsync(query);
        }
    }

    private void OnVideoItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnLoadMoreClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || string.IsNullOrEmpty(_lastQuery)) return;
        LoadMoreButton.IsEnabled = false;
        try
        {
            var moreResults = await ViewModel.SearchMoreAsync(_lastQuery);
            if (moreResults != null && moreResults.Count > 0)
            {
                foreach (var video in moreResults)
                {
                    var vm = new VideoViewModel();
                    vm.UpdateFrom(video);
                    ViewModel.SearchResults.Add(vm);
                }
                LoadMoreButton.Visibility = moreResults.Count >= 20 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                LoadMoreButton.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            LoadMoreButton.IsEnabled = true;
        }
    }
}
