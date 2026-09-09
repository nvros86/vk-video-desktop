namespace VKVideoDesktop.App.ViewModels;

public sealed class HistoryEntryViewModel : ViewModelBase
{
    private string _videoId = string.Empty;
    private string _title = string.Empty;
    private string _author = string.Empty;
    private string _thumbnailUrl = string.Empty;
    private string _durationText = string.Empty;
    private string _lastPositionText = string.Empty;
    private double _progress;

    public string VideoId { get => _videoId; set => SetProperty(ref _videoId, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Author { get => _author; set => SetProperty(ref _author, value); }
    public string ThumbnailUrl { get => _thumbnailUrl; set => SetProperty(ref _thumbnailUrl, value); }
    public string DurationText { get => _durationText; set => SetProperty(ref _durationText, value); }
    public string LastPositionText { get => _lastPositionText; set => SetProperty(ref _lastPositionText, value); }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
}
