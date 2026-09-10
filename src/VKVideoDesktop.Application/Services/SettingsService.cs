using System.Security.Cryptography;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private UserSettings _settings = new();

    public UserSettings Settings => _settings;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "VKVideoDesktop");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public async Task LoadAsync()
    {
        if (File.Exists(_settingsPath))
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            _settings = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();

            if (!string.IsNullOrEmpty(_settings.AccessToken))
            {
                try
                {
                    var encryptedBytes = Convert.FromBase64String(_settings.AccessToken);
                    _settings.AccessToken = DecryptToken(encryptedBytes);
                }
                catch
                {
                    _settings.AccessToken = "";
                }
            }
        }
        else
        {
            _settings = new UserSettings();
        }
    }

    public async Task SaveAsync()
    {
        var plaintext = _settings.AccessToken;
        if (!string.IsNullOrEmpty(plaintext))
        {
            _settings.AccessToken = Convert.ToBase64String(EncryptToken(plaintext));
        }

        var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_settingsPath, json);

        _settings.AccessToken = plaintext;
    }

    public Task ResetAsync()
    {
        _settings = new UserSettings();
        return Task.CompletedTask;
    }

    private static byte[] EncryptToken(string token)
    {
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(token);
        return ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
    }

    private static string DecryptToken(byte[] encryptedBytes)
    {
        var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
