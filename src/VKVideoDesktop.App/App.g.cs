using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace VKVideoDesktop.App;

public sealed partial class App
{
    internal void InitializeComponent()
    {
        try
        {
            var appXamlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "App.xaml");
            if (System.IO.File.Exists(appXamlPath))
            {
                var xaml = System.IO.File.ReadAllText(appXamlPath);
                var obj = Microsoft.UI.Xaml.Markup.XamlReader.Load(xaml);
            }
        }
        catch (Exception ex)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VKVideoDesktop", "log");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "debug.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [App.g] InitializeComponent failed: {ex.GetType().Name}: {ex.Message}\n");
            }
            catch { }
        }
    }
}
