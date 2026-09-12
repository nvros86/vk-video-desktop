using Microsoft.Web.WebView2.Core;
using System.IO;

namespace VKVideoDesktop.App;

internal static class WebView2Helper
{
    private static CoreWebView2Environment? _sharedEnvironment;

    public static async System.Threading.Tasks.Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        if (_sharedEnvironment != null)
            return _sharedEnvironment;

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VKVideoDesktop", "WebView2");

        _sharedEnvironment = await CoreWebView2Environment.CreateWithOptionsAsync(
            null, userDataFolder, new CoreWebView2EnvironmentOptions());
        return _sharedEnvironment;
    }
}
