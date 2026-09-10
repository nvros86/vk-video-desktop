using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Data.Database;
using VKVideoDesktop.Infrastructure.Cache;
using VKVideoDesktop.Infrastructure.Download;
using VKVideoDesktop.Infrastructure.VkApi;

namespace VKVideoDesktop.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly IHost _host;
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient();
                services.AddLogging();

                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAuthenticationService, VkAuthenticationService>();
                services.AddSingleton<IVideoProvider, VkVideoProvider>();
                services.AddSingleton<IDownloadEngine, DownloadEngine>();
                services.AddSingleton<IDownloadRepository, DownloadDatabase>();
                services.AddSingleton<IDownloadSourceResolver, VkVideoSourceResolver>();
                services.AddSingleton<IDownloadManager, DownloadManager>();
                services.AddSingleton<IThumbnailCache, ThumbnailCache>();
                services.AddSingleton<IHistoryService, HistoryDatabase>();
                services.AddSingleton<IFavoritesService, FavoritesDatabase>();
                services.AddSingleton<IPlaylistService, PlaylistDatabase>();
                services.AddSingleton<PlaybackService>();

                services.AddTransient<SearchService>();
                services.AddTransient<VideoService>();
                services.AddTransient<DownloadService>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        Services = _host.Services;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await _host.StartAsync();

        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }

    public static T GetService<T>() where T : class
    {
        return Services.GetRequiredService<T>();
    }
}
