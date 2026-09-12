using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Infrastructure.VkApi;

namespace VKVideoDesktop.App.Views;

public sealed partial class LoginPage : Page
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<LoginPage> _logger;
    private readonly LocalizationService _localization;
    private const string OAuthRedirectPrefix = "https://oauth.vk.com/blank.html";
    private bool _webViewReady;

    public LoginPage()
    {
        _logger = App.GetService<ILogger<LoginPage>>();
        _logger.LogInformation("[LoginPage] Constructor");
        InitializeComponent();
        _authService = App.GetService<IAuthenticationService>();
        _localization = App.GetService<LocalizationService>();
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        ShowState(WebViewState);
        WebViewLoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            var authService = _authService as VkAuthenticationService;
            var authUrl = authService?.GetAuthUrl();
            if (!string.IsNullOrEmpty(authUrl))
            {
                await InitializeWebViewAsync(authUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoginPage] Failed to start OAuth");
            ShowError(_localization["LoginErrorAuthFailed"]);
        }
    }

    private async System.Threading.Tasks.Task InitializeWebViewAsync(string authUrl)
    {
        try
        {
            var env = await WebView2Helper.GetEnvironmentAsync();
            await LoginWebView.EnsureCoreWebView2Async(env);
            _webViewReady = true;
            LoginWebView.CoreWebView2.NavigationStarting += OnWebViewNavigationStarting;
            LoginWebView.CoreWebView2.NavigationCompleted += OnWebViewNavigationCompleted;
            LoginWebView.CoreWebView2.Navigate(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoginPage] WebView2 init failed - showing browser fallback");
            _webViewReady = false;
            OpenInBrowserButton.Visibility = Visibility.Visible;
            ShowError("WebView2 не установлен. Установите WebView2 Runtime или используйте кнопку «Открыть в браузере».");
        }
    }

    private void OnWebViewNavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            WebViewLoadingOverlay.Visibility = Visibility.Collapsed;
        });
    }

    private void OnWebViewNavigationStarting(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Uri))
            return;

        if (e.Uri.StartsWith(OAuthRedirectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;

            var fragment = e.Uri.Contains('#') ? e.Uri.Substring(e.Uri.IndexOf('#') + 1) : "";
            var token = ExtractTokenFromFragment(fragment);

            if (!string.IsNullOrEmpty(token))
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    ShowState(LoadingState);
                    var success = await LoginWithTokenAsync(token);
                    if (success)
                    {
                        ShowState(SuccessState);
                        await System.Threading.Tasks.Task.Delay(800);
                        var mainWindow = App.GetService<MainWindow>();
                        mainWindow.NavigateToHome();
                    }
                    else
                    {
                        ShowError(_localization["LoginErrorAuthFailed"]);
                    }
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ShowError(_localization["LoginErrorExtractFailed"]);
                });
            }
        }
    }

    private static string? ExtractTokenFromFragment(string fragment)
    {
        if (string.IsNullOrWhiteSpace(fragment))
            return null;

        var pairs = fragment.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "access_token" && !string.IsNullOrWhiteSpace(kv[1]))
            {
                return kv[1];
            }
        }
        return null;
    }

    private async System.Threading.Tasks.Task<bool> LoginWithTokenAsync(string token)
    {
        try
        {
            if (_authService is VkAuthenticationService vkAuth)
            {
                return await vkAuth.LoginAsync(token);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoginPage] Login failed");
            return false;
        }
    }

    private void OnBackToIntroClick(object sender, RoutedEventArgs e)
    {
        if (_webViewReady && LoginWebView.CoreWebView2 != null)
        {
            LoginWebView.CoreWebView2.NavigationStarting -= OnWebViewNavigationStarting;
            LoginWebView.CoreWebView2.NavigationCompleted -= OnWebViewNavigationCompleted;
            LoginWebView.CoreWebView2.Navigate("about:blank");
        }
        WebViewLoadingOverlay.Visibility = Visibility.Collapsed;
        ShowState(IntroState);
    }

    private async void OnOpenInBrowserClick(object sender, RoutedEventArgs e)
    {
        if (_authService is VkAuthenticationService vkAuth)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(vkAuth.GetAuthUrl()));
            Step2Text.Visibility = Visibility.Visible;
        }
    }

    private void ShowState(UIElement state)
    {
        IntroState.Visibility = Visibility.Collapsed;
        WebViewState.Visibility = Visibility.Collapsed;
        LoadingState.Visibility = Visibility.Collapsed;
        SuccessState.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Collapsed;
        state.Visibility = Visibility.Visible;
    }

    private void ShowError(string message)
    {
        ErrorDetailText.Text = message;
        ShowState(ErrorState);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _logger.LogInformation("[LoginPage] OnNavigatedTo");
        base.OnNavigatedTo(e);
        if (e.Parameter is string error && !string.IsNullOrEmpty(error))
        {
            ShowError(error);
        }
    }
}
