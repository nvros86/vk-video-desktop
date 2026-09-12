using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.App.ViewModels;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;

namespace VKVideoDesktop.App.Views;

public sealed partial class ChannelPage : Page
{
    private readonly IVideoProvider _videoProvider;
    private readonly ILogger<ChannelPage> _logger;
    private readonly LocalizationService _localization;

    public ChannelPage()
    {
        _logger = App.GetService<ILogger<ChannelPage>>();
        _logger.LogInformation("[ChannelPage] Constructor");
        InitializeComponent();
        _videoProvider = App.GetService<IVideoProvider>();
        _localization = App.GetService<LocalizationService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _logger.LogInformation("[ChannelPage] OnNavigatedTo");
        base.OnNavigatedTo(e);

        if (e.Parameter is string channelId)
        {
            var channel = await _videoProvider.GetChannelAsync(channelId, CancellationToken.None);
            if (channel != null)
            {
                ChannelNameText.Text = channel.Name;
                DescriptionText.Text = channel.Description;
                SubscriberCountText.Text = string.Format(_localization["ChannelSubscriberCount"], channel.SubscriberCount.ToString("N0"));

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
