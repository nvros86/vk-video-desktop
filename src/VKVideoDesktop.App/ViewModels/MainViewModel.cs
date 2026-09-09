using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly SearchService _searchService;
    private readonly VideoService _videoService;
    private readonly IHistoryService _historyService;
    private readonly ILogger<MainViewModel> _logger;

    private string _searchQuery = string.Empty;
    private bool _isLoading;
    private string _currentSection = "Главная";

    public MainViewModel(
        SearchService searchService,
        VideoService videoService,
        IHistoryService historyService,
        ILogger<MainViewModel> logger)
    {
        _searchService = searchService;
        _videoService = videoService;
        _historyService = historyService;
        _logger = logger;
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

    public bool HasHistory => HistoryEntries.Count > 0;

    public ObservableCollection<VideoViewModel> Videos { get; } = new();
    public ObservableCollection<VideoViewModel> Recommendations { get; } = new();
    public ObservableCollection<ChannelViewModel> Channels { get; } = new();
    public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; } = new();
    public ObservableCollection<VideoViewModel> SearchResults { get; } = new();

    public async Task LoadRecommendationsAsync()
    {
        try
        {
            IsLoading = true;

            var recommendations = await _searchService.GetRecommendationsAsync();
            Recommendations.Clear();
            foreach (var video in recommendations)
            {
                var vm = new VideoViewModel();
                vm.UpdateFrom(video);
                Recommendations.Add(vm);
            }

            // Load history
            var history = await _historyService.GetAllAsync();
            HistoryEntries.Clear();
            foreach (var entry in history.Take(10))
            {
                HistoryEntries.Add(new HistoryEntryViewModel
                {
                    VideoId = entry.VideoId,
                    Title = entry.Title,
                    Author = entry.Author,
                    ThumbnailUrl = entry.ThumbnailUrl,
                    DurationText = FormatDuration(entry.Duration),
                    LastPositionText = $"Продолжить с {FormatDuration(entry.LastPosition)}",
                    Progress = entry.Duration.TotalSeconds > 0
                        ? entry.LastPosition.TotalSeconds / entry.Duration.TotalSeconds * 100
                        : 0
                });
            }

            OnPropertyChanged(nameof(HasHistory));
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

    public async Task SearchAsync(string query)
    {
        try
        {
            IsLoading = true;
            var result = await _searchService.SearchAsync(query);
            SearchResults.Clear();
            foreach (var video in result.Videos)
            {
                var vm = new VideoViewModel();
                vm.UpdateFrom(video);
                SearchResults.Add(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for '{Query}'", query);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}"
            : $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
    }
}
