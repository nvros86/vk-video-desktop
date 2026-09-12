using VKVideoDesktop.Application.Services;

namespace VKVideoDesktop.App.ViewModels;

public sealed class PlaylistViewModel : ViewModelBase
{
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private int _videoCount;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public int VideoCount { get => _videoCount; set => SetProperty(ref _videoCount, value); }
    public string VideoCountText => string.Format(App.GetService<LocalizationService>()["PlaylistVideoCount"], VideoCount);
}
