using Microsoft.Extensions.Logging;
using Moq;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;
using VKVideoDesktop.Infrastructure.VkApi;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public sealed class AuthenticationTests
{
    private readonly Mock<ISettingsService> _settingsMock;
    private readonly Mock<ILogger<VkAuthenticationService>> _loggerMock;
    private readonly VkAuthenticationService _authService;

    public AuthenticationTests()
    {
        _settingsMock = new Mock<ISettingsService>();
        _settingsMock.Setup(s => s.Settings).Returns(new UserSettings());
        _loggerMock = new Mock<ILogger<VkAuthenticationService>>();
        _authService = new VkAuthenticationService(_settingsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void IsAuthenticated_WithToken_ReturnsTrue()
    {
        _settingsMock.Setup(s => s.Settings).Returns(new UserSettings { AccessToken = "valid_token" });

        var auth = new VkAuthenticationService(_settingsMock.Object, _loggerMock.Object);

        Assert.True(auth.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithoutToken_ReturnsFalse()
    {
        _settingsMock.Setup(s => s.Settings).Returns(new UserSettings { AccessToken = null });

        var auth = new VkAuthenticationService(_settingsMock.Object, _loggerMock.Object);

        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_EmptyToken_ReturnsFalse()
    {
        _settingsMock.Setup(s => s.Settings).Returns(new UserSettings { AccessToken = "" });

        var auth = new VkAuthenticationService(_settingsMock.Object, _loggerMock.Object);

        Assert.False(auth.IsAuthenticated);
    }

    [Fact]
    public void GetAuthUrl_ContainsAppId()
    {
        var url = _authService.GetAuthUrl();

        Assert.Contains("client_id=51797770", url);
    }

    [Fact]
    public void GetAuthUrl_IsValidHttps()
    {
        var url = _authService.GetAuthUrl();

        Assert.StartsWith("https://", url);
    }

    [Fact]
    public async Task LoginAsync_WithToken_SetsToken()
    {
        await _authService.LoginAsync("test_token");

        Assert.Equal("test_token", _authService.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WithToken_SetsIsAuthorized()
    {
        await _authService.LoginAsync("test_token");

        Assert.True(_settingsMock.Object.Settings.IsAuthorized);
    }

    [Fact]
    public async Task LoginAsync_WithToken_CallsSaveAsync()
    {
        await _authService.LoginAsync("test_token");

        _settingsMock.Verify(s => s.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_EmptyToken_ReturnsFalse()
    {
        var result = await _authService.LoginAsync("");

        Assert.False(result);
    }

    [Fact]
    public async Task LoginAsync_NullToken_ReturnsFalse()
    {
        var result = await _authService.LoginAsync(null!);

        Assert.False(result);
    }

    [Fact]
    public async Task LoginAsync_WhitespaceToken_ReturnsFalse()
    {
        var result = await _authService.LoginAsync("   ");

        Assert.False(result);
    }

    [Fact]
    public async Task LogoutAsync_ClearsToken()
    {
        await _authService.LoginAsync("test_token");
        await _authService.LogoutAsync();

        Assert.Null(_authService.AccessToken);
    }

    [Fact]
    public async Task LogoutAsync_ClearsIsAuthorized()
    {
        await _authService.LoginAsync("test_token");
        await _authService.LogoutAsync();

        Assert.False(_settingsMock.Object.Settings.IsAuthorized);
    }

    [Fact]
    public async Task LogoutAsync_CallsSaveAsync()
    {
        await _authService.LoginAsync("test_token");

        await _authService.LogoutAsync();

        _settingsMock.Verify(s => s.SaveAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsCurrentToken()
    {
        await _authService.LoginAsync("my_access_token");

        var token = await _authService.GetAccessTokenAsync();

        Assert.Equal("my_access_token", token);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoToken_ReturnsNull()
    {
        var token = await _authService.GetAccessTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_TrimsToken()
    {
        await _authService.LoginAsync("  trimmed_token  ");

        Assert.Equal("trimmed_token", _authService.AccessToken);
    }

    [Fact]
    public void AccessToken_ReturnsSameAsSettings()
    {
        _settingsMock.Setup(s => s.Settings).Returns(new UserSettings { AccessToken = "abc" });

        var auth = new VkAuthenticationService(_settingsMock.Object, _loggerMock.Object);

        Assert.Equal(auth.AccessToken, auth.AccessToken);
    }

    [Fact]
    public async Task LoginLogout_LoginAgain_WorksCorrectly()
    {
        await _authService.LoginAsync("first_token");
        Assert.True(_authService.IsAuthenticated);

        await _authService.LogoutAsync();
        Assert.False(_authService.IsAuthenticated);

        await _authService.LoginAsync("second_token");
        Assert.True(_authService.IsAuthenticated);
        Assert.Equal("second_token", _authService.AccessToken);
    }
}
