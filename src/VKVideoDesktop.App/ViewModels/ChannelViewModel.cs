namespace VKVideoDesktop.App.ViewModels;

public sealed class ChannelViewModel : ViewModelBase
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _avatarUrl = string.Empty;
    private string _subscriberCountText = string.Empty;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string AvatarUrl { get => _avatarUrl; set => SetProperty(ref _avatarUrl, value); }
    public string SubscriberCountText { get => _subscriberCountText; set => SetProperty(ref _subscriberCountText, value); }
}
