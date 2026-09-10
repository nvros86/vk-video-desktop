using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class ChannelPage : Page
{
    private readonly IVideoProvider _videoProvider;

    public ChannelPage()
    {
        InitializeComponent();
        _videoProvider = App.GetService<IVideoProvider>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string channelId)
        {
            var channel = await _videoProvider.GetChannelAsync(channelId, CancellationToken.None);
            if (channel != null)
            {
                ChannelNameText.Text = channel.Name;
                ChannelUsernameText.Text = $"@{channel.Username}";
                DescriptionText.Text = channel.Description;
                SubscriberCountText.Text = $"{channel.SubscriberCount:N0} подписчиков";

                if (!string.IsNullOrEmpty(channel.AvatarUrl))
                {
                    AvatarBrush.ImageSource = new BitmapImage(new Uri(channel.AvatarUrl));
                }

                if (!string.IsNullOrEmpty(channel.BannerUrl))
                {
                    BannerImage.Source = new BitmapImage(new Uri(channel.BannerUrl));
                }

                var videos = await _videoProvider.GetVideosByChannelAsync(channelId, CancellationToken.None);
                ChannelVideos.Items.Clear();
                foreach (var video in videos)
                {
                    var vm = new VideoViewModel();
                    vm.UpdateFrom(video);
                    ChannelVideos.Items.Add(vm);
                }
            }
        }
    }

    private void OnVideoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private void OnContextPlay(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnContextFavorite(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            var favorites = App.GetService<IFavoritesService>();
            var existing = await favorites.GetAllAsync();
            if (existing.Any(f => f.VideoId == videoId))
            {
                await favorites.RemoveAsync(videoId);
            }
            else
            {
                await favorites.AddAsync(new Core.Models.FavoriteEntry
                {
                    VideoId = videoId,
                    Title = "Video",
                    Author = "",
                    AddedAt = DateTime.UtcNow
                });
            }
        }
    }

    private void OnContextDownload(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            Frame.Navigate(typeof(VideoPage), videoId);
        }
    }

    private async void OnContextOpenInVk(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is string videoId)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://vk.com/video{videoId}"));
        }
    }
}
