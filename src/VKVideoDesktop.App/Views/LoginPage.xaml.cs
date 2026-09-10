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

    private async void OnLoginClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_authService is VkAuthenticationService vkAuth)
        {
            await Launcher.LaunchUriAsync(new Uri(vkAuth.GetAuthUrl()));
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string error && !string.IsNullOrEmpty(error))
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }
}
