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
        }
        else
        {
            _settings = new UserSettings();
        }
    }

    public async Task SaveAsync()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_settingsPath, json);
    }

    public Task ResetAsync()
    {
        _settings = new UserSettings();
        return Task.CompletedTask;
    }
}
