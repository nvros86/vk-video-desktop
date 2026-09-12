using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Application.Services;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Models;
using Windows.System;

namespace VKVideoDesktop.App.Views;

public sealed partial class WebViewVideoPage : Page
{
    private string? _videoUrl;
    private readonly ILogger<WebViewVideoPage> _logger;
    private readonly LocalizationService _localization;

    public WebViewVideoPage()
    {
        _logger = App.GetService<ILogger<WebViewVideoPage>>();
        _logger.LogInformation("[WebViewVideoPage] Constructor");
        InitializeComponent();
        _localization = App.GetService<LocalizationService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _logger.LogInformation("[WebViewVideoPage] OnNavigatedTo");
        base.OnNavigatedTo(e);

        if (e.Parameter is Video video)
        {
            TitleText.Text = video.Title ?? "Video";
            _videoUrl = video.PlaybackUrl;

            if (!string.IsNullOrEmpty(_videoUrl))
            {
                try
                {
                    var env = await WebView2Helper.GetEnvironmentAsync();
                    await WebView.EnsureCoreWebView2Async(env);
                    WebView.CoreWebView2.Navigate(_videoUrl);
                }
                catch (Exception)
                {
                    await Launcher.LaunchUriAsync(new Uri(_videoUrl));
                }
            }
            else
            {
                TitleText.Text = _localization["VideoUnavailableWeb"];
            }
        }
        else if (e.Parameter is string url)
        {
            _videoUrl = url;
            try
            {
                var env = await WebView2Helper.GetEnvironmentAsync();
                await WebView.EnsureCoreWebView2Async(env);
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
