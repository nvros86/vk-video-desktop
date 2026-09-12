using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace VKVideoDesktop.Application.Services;

public sealed class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private readonly object _lock = new();

    public string CurrentLanguage { get; private set; } = "ru-RU";
    public IReadOnlyList<string> AvailableLanguages { get; } = new[] { "ru-RU", "en-US" };

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        var systemLang = CultureInfo.CurrentUICulture.Name;
        if (systemLang.StartsWith("ru"))
            CurrentLanguage = "ru-RU";
        else
            CurrentLanguage = "en-US";

        LoadResources("ru-RU");
        LoadResources("en-US");
    }

    public string this[string key]
    {
        get
        {
            lock (_lock)
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
    }

    public void SetLanguage(string language)
    {
        if (!AvailableLanguages.Contains(language) || language == CurrentLanguage)
            return;

        CurrentLanguage = language;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadResources(string language)
    {
        var basePath = AppContext.BaseDirectory;
        var path = Path.Combine(basePath, "Resources", "Strings", language, "Resources.resw");
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

            lock (_lock)
            {
                _resources[language] = resources;
            }
        }
        catch
        {
        }
    }
}
