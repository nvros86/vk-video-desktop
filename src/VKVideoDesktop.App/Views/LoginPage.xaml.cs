using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Infrastructure.VkApi;
using Windows.System;

namespace VKVideoDesktop.App.Views;

public sealed partial class LoginPage : Page
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<LoginPage> _logger;
    private readonly LocalizationService _localization;

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
        if (_authService is VkAuthenticationService vkAuth)
        {
            await Launcher.LaunchUriAsync(new Uri(vkAuth.GetAuthUrl()));
        }

        Step1Text.Visibility = Visibility.Collapsed;
        LoginButton.Visibility = Visibility.Collapsed;
        Step2Text.Visibility = Visibility.Visible;
        TokenInput.Visibility = Visibility.Visible;
        SubmitTokenButton.Visibility = Visibility.Visible;
        TokenInput.Focus(FocusState.Programmatic);
    }

    private async void OnSubmitTokenClick(object sender, RoutedEventArgs e)
    {
        var input = TokenInput.Text?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            ShowError(_localization["LoginErrorEmptyToken"]);
            return;
        }

        var token = ExtractToken(input);

        if (string.IsNullOrEmpty(token))
        {
            ShowError(_localization["LoginErrorExtractFailed"]);
            return;
        }

        LoadingText.Visibility = Visibility.Visible;
        ErrorText.Visibility = Visibility.Collapsed;

        if (_authService is VkAuthenticationService vkAuth)
        {
            var success = await vkAuth.LoginAsync(token);
            if (success)
            {
                var mainWindow = App.GetService<MainWindow>();
                mainWindow.NavigateToHome();
            }
            else
            {
                ShowError(_localization["LoginErrorAuthFailed"]);
            }
        }

        LoadingText.Visibility = Visibility.Collapsed;
    }

    private static string? ExtractToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (input.Contains("access_token="))
        {
            var match = Regex.Match(input, @"access_token=([^&]+)");
            if (match.Success)
                return match.Groups[1].Value;
        }

        if (input.Length >= 80 && !input.Contains(" "))
            return input;

        return null;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
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
