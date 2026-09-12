using System.Security.Cryptography;
using System.Text.Json;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Application.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
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
        if (!File.Exists(_settingsPath))
        {
            _settings = new UserSettings();
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            _settings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            _settings = new UserSettings();
            return;
        }

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

    public async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var snapshot = new UserSettings
            {
                AccessToken = _settings.AccessToken,
                IsAuthorized = _settings.IsAuthorized,
                Language = _settings.Language,
                Theme = _settings.Theme,
                StartWithWindows = _settings.StartWithWindows,
                MinimizeToTray = _settings.MinimizeToTray,
                EnableNotifications = _settings.EnableNotifications,
                DefaultQuality = _settings.DefaultQuality,
                Autoplay = _settings.Autoplay,
                DefaultVolume = _settings.DefaultVolume,
                DefaultPlaybackSpeed = _settings.DefaultPlaybackSpeed,
                DownloadFolder = _settings.DownloadFolder,
                MaxConcurrentDownloads = _settings.MaxConcurrentDownloads,
                SpeedLimit = _settings.SpeedLimit,
                RetryCount = _settings.RetryCount,
                AutoResumeAfterStartup = _settings.AutoResumeAfterStartup,
                DeletePartOnCancel = _settings.DeletePartOnCancel,
                AskFolderBeforeDownload = _settings.AskFolderBeforeDownload,
                DownloadQualityBehavior = _settings.DownloadQualityBehavior,
                UseProxy = _settings.UseProxy,
                ProxyAddress = _settings.ProxyAddress
            };

            if (!string.IsNullOrEmpty(snapshot.AccessToken))
            {
                snapshot.AccessToken = Convert.ToBase64String(EncryptToken(snapshot.AccessToken));
            }

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempPath = _settingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public async Task ResetAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            _settings = new UserSettings();

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempPath = _settingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        finally
        {
            _saveLock.Release();
        }
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
