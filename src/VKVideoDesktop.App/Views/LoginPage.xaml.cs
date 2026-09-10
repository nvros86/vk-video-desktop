using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Infrastructure.VkApi;
using Windows.System;

namespace VKVideoDesktop.App.Views;

public sealed partial class LoginPage : Page
{
    private readonly IAuthenticationService _authService;

    public LoginPage()
    {
        InitializeComponent();
        _authService = App.GetService<IAuthenticationService>();
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
            ShowError("Вставьте URL или токен");
            return;
        }

        var token = ExtractToken(input);

        if (string.IsNullOrEmpty(token))
        {
            ShowError("Не удалось извлечь токен. Вставьте полный URL из адресной строки.");
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
                ShowError("Ошибка авторизации. Проверьте токен и попробуйте снова.");
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
        base.OnNavigatedTo(e);
        if (e.Parameter is string error && !string.IsNullOrEmpty(error))
        {
            ShowError(error);
        }
    }
}
