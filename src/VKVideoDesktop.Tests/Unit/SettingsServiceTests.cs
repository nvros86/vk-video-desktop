using System.Reflection;
using System.Security.Cryptography;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Models;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public sealed class SettingsServiceExtendedTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempSettingsPath;

    public SettingsServiceExtendedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"vkd_settings_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _tempSettingsPath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // cleanup best-effort
        }
    }

    private SettingsService CreateService()
    {
        var service = new SettingsService();
        var field = typeof(SettingsService).GetField("_settingsPath", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(service, _tempSettingsPath);
        return service;
    }

    private static void AssertDefaultSettings(UserSettings s)
    {
        Assert.Equal("ru", s.Language);
        Assert.Equal(AppTheme.Dark, s.Theme);
        Assert.False(s.StartWithWindows);
        Assert.True(s.MinimizeToTray);
        Assert.True(s.EnableNotifications);
        Assert.Equal("1080p", s.DefaultQuality);
        Assert.True(s.Autoplay);
        Assert.Equal(1.0, s.DefaultVolume);
        Assert.Equal(1.0, s.DefaultPlaybackSpeed);
        Assert.Equal(2, s.MaxConcurrentDownloads);
        Assert.Equal(0L, s.SpeedLimit);
        Assert.Equal(3, s.RetryCount);
        Assert.True(s.AutoResumeAfterStartup);
        Assert.True(s.DeletePartOnCancel);
        Assert.False(s.AskFolderBeforeDownload);
        Assert.Equal(DownloadQualityBehavior.Ask, s.DownloadQualityBehavior);
        Assert.False(s.UseProxy);
        Assert.False(s.IsAuthorized);
        Assert.Null(s.AccessToken);
    }

    [Fact]
    public async Task ResetAsync_RestoresDefaults()
    {
        var service = CreateService();
        service.Settings.Language = "en";
        service.Settings.Theme = AppTheme.Light;
        service.Settings.MaxConcurrentDownloads = 5;
        service.Settings.AccessToken = "secret";

        await service.ResetAsync();

        AssertDefaultSettings(service.Settings);
    }

    [Fact]
    public async Task ResetAsync_ThenSaveAsync_PersistsDefaults()
    {
        var service = CreateService();
        service.Settings.Language = "en";
        service.Settings.Theme = AppTheme.Light;

        await service.ResetAsync();
        await service.SaveAsync();

        var service2 = CreateService();
        await service2.LoadAsync();

        AssertDefaultSettings(service2.Settings);
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_DoesNotThrow()
    {
        await File.WriteAllTextAsync(_tempSettingsPath, "{ this is not valid json }}}");

        var service = CreateService();
        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal("ru", service.Settings.Language);
    }

    [Fact]
    public async Task LoadAsync_EmptyFile_DoesNotThrow()
    {
        await File.WriteAllTextAsync(_tempSettingsPath, string.Empty);

        var service = CreateService();
        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal("ru", service.Settings.Language);
    }

    [Fact]
    public async Task LoadAsync_BinaryGarbage_DoesNotThrow()
    {
        var garbage = new byte[256];
        RandomNumberGenerator.Fill(garbage);
        await File.WriteAllBytesAsync(_tempSettingsPath, garbage);

        var service = CreateService();
        await service.LoadAsync();

        Assert.NotNull(service.Settings);
        Assert.Equal("ru", service.Settings.Language);
    }

    [Fact]
    public async Task SaveAsync_ConcurrentCalls_DoesNotCorrupt()
    {
        var service = CreateService();

        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            return Task.Run(async () =>
            {
                service.Settings.Language = $"lang_{i}";
                service.Settings.MaxConcurrentDownloads = i;
                await service.SaveAsync();
            });
        });

        await Task.WhenAll(tasks);

        Assert.True(File.Exists(_tempSettingsPath));
        var json = await File.ReadAllTextAsync(_tempSettingsPath);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var service2 = CreateService();
        await service2.LoadAsync();
        Assert.NotNull(service2.Settings);
    }

    [Fact]
    public async Task SaveAsync_CreatesFileIfNotExists()
    {
        Assert.False(File.Exists(_tempSettingsPath));

        var service = CreateService();
        await service.SaveAsync();

        Assert.True(File.Exists(_tempSettingsPath));
    }

    [Fact]
    public async Task SaveAsync_AtomicWrite_TmpFileRemoved()
    {
        var service = CreateService();
        await service.SaveAsync();

        var tmpPath = _tempSettingsPath + ".tmp";
        Assert.False(File.Exists(tmpPath));
        Assert.True(File.Exists(_tempSettingsPath));
    }

    [Fact]
    public async Task SaveAsync_PreservesExistingSettings()
    {
        var service = CreateService();
        service.Settings.Language = "en";
        service.Settings.Theme = AppTheme.Light;
        service.Settings.MaxConcurrentDownloads = 7;

        await service.SaveAsync();

        var service2 = CreateService();
        await service2.LoadAsync();

        Assert.Equal("en", service2.Settings.Language);
        Assert.Equal(AppTheme.Light, service2.Settings.Theme);
        Assert.Equal(7, service2.Settings.MaxConcurrentDownloads);
    }

    [Fact]
    public void SettingsProperty_ChangesAreTracked()
    {
        var service = CreateService();
        Assert.Equal("ru", service.Settings.Language);

        service.Settings.Language = "en";
        Assert.Equal("en", service.Settings.Language);

        service.Settings.Language = "de";
        Assert.Equal("de", service.Settings.Language);
    }

    [Fact]
    public async Task SaveAsync_WithAccessToken_DpapiEncryptsToken()
    {
        var service = CreateService();
        const string plainToken = "my_secret_access_token";
        service.Settings.AccessToken = plainToken;

        await service.SaveAsync();

        var rawJson = await File.ReadAllTextAsync(_tempSettingsPath);
        Assert.DoesNotContain(plainToken, rawJson);

        var service2 = CreateService();
        await service2.LoadAsync();
        Assert.Equal(plainToken, service2.Settings.AccessToken);
    }

    [Fact]
    public async Task LoadAsync_TokenDecryptedCorrectly()
    {
        var service = CreateService();
        service.Settings.AccessToken = "dpapi_test_token_12345";
        await service.SaveAsync();

        var service2 = CreateService();
        await service2.LoadAsync();

        Assert.Equal("dpapi_test_token_12345", service2.Settings.AccessToken);
    }
}
