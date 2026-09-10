using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace VKVideoDesktop.Application.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private readonly string _resourcesPath;

    public string CurrentLanguage { get; private set; } = "ru-RU";
    public IReadOnlyList<string> AvailableLanguages { get; } = new[] { "ru-RU", "en-US" };

    public event EventHandler? LanguageChanged;

    public LocalizationService(string resourcesPath)
    {
        _resourcesPath = resourcesPath;
        LoadResources("en-US");
        LoadResources("ru-RU");

        var systemLang = CultureInfo.CurrentUICulture.Name;
        if (systemLang.StartsWith("ru"))
            CurrentLanguage = "ru-RU";
        else
            CurrentLanguage = "en-US";
    }

    public string this[string key]
    {
        get
        {
            if (_resources.TryGetValue(CurrentLanguage, out var lang) &&
                lang.TryGetValue(key, out var value))
                return value;

            if (_resources.TryGetValue("en-US", out var fallback) &&
                fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }
    }

    public void SetLanguage(string language)
    {
        if (AvailableLanguages.Contains(language) && language != CurrentLanguage)
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void LoadResources(string language)
    {
        var path = Path.Combine(_resourcesPath, "Resources", "Strings", language, "Resources.resw");
        if (!File.Exists(path)) return;

        try
        {
            var doc = XDocument.Load(path);
            var resources = new Dictionary<string, string>();

            if (doc.Root != null)
            {
                foreach (var data in doc.Root.Elements("data"))
                {
                    var name = data.Attribute("name")?.Value;
                    var value = data.Element("value")?.Value;
                    if (!string.IsNullOrEmpty(name) && value != null)
                        resources[name] = value;
                }
            }

            _resources[language] = resources;
        }
        catch
        {
        }
    }
}
