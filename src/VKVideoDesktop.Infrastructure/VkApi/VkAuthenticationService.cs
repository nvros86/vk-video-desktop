using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Interfaces;


namespace VKVideoDesktop.Infrastructure.VkApi;

public sealed class VkAuthenticationService : IAuthenticationService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<VkAuthenticationService> _logger;
    private const int AppId = 51797770;

    public VkAuthenticationService(ISettingsService settingsService, ILogger<VkAuthenticationService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_settingsService.Settings.AccessToken);

    public string? AccessToken => _settingsService.Settings.AccessToken;

    public string GetAuthUrl()
    {
        return $"https://oauth.vk.com/authorize?client_id={AppId}&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=video,offline&response_type=token&v=5.199";
    }

    public async Task<bool> LoginAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("[AuthService] LoginAsync - empty token");
                return false;
            }

            _settingsService.Settings.AccessToken = token.Trim();
            _settingsService.Settings.IsAuthorized = true;
            await _settingsService.SaveAsync();

            _logger.LogInformation("[AuthService] LoginAsync - success");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuthService] LoginAsync failed");
            return false;
        }
    }

    Task<bool> IAuthenticationService.LoginAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(IsAuthenticated);
    }

    public async Task LogoutAsync()
    {
        _settingsService.Settings.AccessToken = null;
        _settingsService.Settings.IsAuthorized = false;
        await _settingsService.SaveAsync();
        _logger.LogInformation("User logged out");
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_settingsService.Settings.AccessToken);
    }
}
