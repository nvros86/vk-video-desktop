using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly SearchService _searchService;
    private readonly VideoService _videoService;
    private readonly ILogger<MainViewModel> _logger;

    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private string _currentSection = "Главная";

    public MainViewModel(
        SearchService searchService,
        VideoService videoService,
        ILogger<MainViewModel> logger)
    {
        _searchService = searchService;
        _videoService = videoService;
        _logger = logger;

        SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync);
        NavigateCommand = new RelayCommand<string>(Navigate);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string CurrentSection
    {
        get => _currentSection;
        set => SetProperty(ref _currentSection, value);
    }

    public ObservableCollection<Video> Videos { get; } = new();
    public ObservableCollection<Video> Recommendations { get; } = new();

    public ICommand SearchCommand { get; }
    public ICommand NavigateCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var recommendations = await _searchService.GetRecommendationsAsync();
            Recommendations.Clear();
            foreach (var video in recommendations)
            {
                Recommendations.Add(video);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recommendations");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        try
        {
            IsLoading = true;
            var result = await _searchService.SearchAsync(SearchQuery);
            Videos.Clear();
            foreach (var video in result.Videos)
            {
                Videos.Add(video);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for '{Query}'", SearchQuery);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Navigate(string? section)
    {
        if (!string.IsNullOrEmpty(section))
        {
            CurrentSection = section;
        }
    }
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;

    public RelayCommand(Action<T?> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is T typed)
            _execute(typed);
        else
            _execute(default);
    }
}
