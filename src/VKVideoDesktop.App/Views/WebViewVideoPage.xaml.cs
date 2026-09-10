using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Core.Models;
using Windows.System;

namespace VKVideoDesktop.App.Views;

public sealed partial class WebViewVideoPage : Page
{
    private string? _videoUrl;

    public WebViewVideoPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Video video)
        {
            TitleText.Text = video.Title ?? "Video";
            _videoUrl = video.PlaybackUrl;

            if (!string.IsNullOrEmpty(_videoUrl))
            {
                try
                {
                    await WebView.EnsureCoreWebView2Async();
                    WebView.CoreWebView2.Navigate(_videoUrl);
                }
                catch (Exception)
                {
                    await Launcher.LaunchUriAsync(new Uri(_videoUrl));
                }
            }
            else
            {
                TitleText.Text = "Video unavailable";
            }
        }
        else if (e.Parameter is string url)
        {
            _videoUrl = url;
            try
            {
                await WebView.EnsureCoreWebView2Async();
                WebView.CoreWebView2.Navigate(url);
            }
            catch (Exception)
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
        }
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private async void OnOpenInBrowser(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_videoUrl))
            await Launcher.LaunchUriAsync(new Uri(_videoUrl));
    }
}
