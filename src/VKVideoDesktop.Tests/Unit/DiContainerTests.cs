using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Application.Services;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Metrics;
using VKVideoDesktop.Data.Database;
using VKVideoDesktop.Infrastructure;
using VKVideoDesktop.Infrastructure.Cache;
using VKVideoDesktop.Infrastructure.Download;
using VKVideoDesktop.Infrastructure.VkApi;
using Xunit;

namespace VKVideoDesktop.Tests.Unit;

public class DiContainerTests
{
    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                services.AddHttpClient();

                services.AddSingleton<LocalizationService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IAuthenticationService, VkAuthenticationService>();
                services.AddSingleton<IVideoProvider, VkVideoProvider>();
                services.AddSingleton<IDownloadEngine, DownloadEngine>();
                services.AddSingleton<IDownloadRepository, DownloadDatabase>();
                services.AddSingleton<IVideoDownloadProvider, VkVideoDownloadProvider>();
                services.AddSingleton<IDownloadManager, DownloadManager>();
                services.AddSingleton<IThumbnailCache, ThumbnailCache>();
                services.AddSingleton<IHistoryService, HistoryDatabase>();
                services.AddSingleton<IFavoritesService, FavoritesDatabase>();
                services.AddSingleton<IPlaylistService, PlaylistDatabase>();
                services.AddSingleton<IVKWebViewService, VKWebViewService>();
                services.AddSingleton<PlaybackService>();
                services.AddSingleton<ErrorHandlerService>();
                services.AddSingleton<DeepLinkService>();
                services.AddTransient<SearchService>();
                services.AddTransient<VideoService>();
                services.AddTransient<DownloadService>();
                services.AddSingleton<NotificationService>();
                services.AddSingleton<AppMetrics>();
            })
            .Build();
    }

    [Fact]
    public void BuildContainer_AllSingletons_ResolveSuccessfully()
    {
        using var host = BuildHost();
        var provider = host.Services;

        Assert.NotNull(provider.GetRequiredService<LocalizationService>());
        Assert.NotNull(provider.GetRequiredService<ISettingsService>());
        Assert.NotNull(provider.GetRequiredService<IAuthenticationService>());
        Assert.NotNull(provider.GetRequiredService<IVideoProvider>());
        Assert.NotNull(provider.GetRequiredService<IDownloadEngine>());
        Assert.NotNull(provider.GetRequiredService<IDownloadRepository>());
        Assert.NotNull(provider.GetRequiredService<IVideoDownloadProvider>());
        Assert.NotNull(provider.GetRequiredService<IDownloadManager>());
        Assert.NotNull(provider.GetRequiredService<IThumbnailCache>());
        Assert.NotNull(provider.GetRequiredService<IHistoryService>());
        Assert.NotNull(provider.GetRequiredService<IFavoritesService>());
        Assert.NotNull(provider.GetRequiredService<IPlaylistService>());
        Assert.NotNull(provider.GetRequiredService<IVKWebViewService>());
        Assert.NotNull(provider.GetRequiredService<PlaybackService>());
        Assert.NotNull(provider.GetRequiredService<ErrorHandlerService>());
        Assert.NotNull(provider.GetRequiredService<DeepLinkService>());
        Assert.NotNull(provider.GetRequiredService<NotificationService>());
        Assert.NotNull(provider.GetRequiredService<AppMetrics>());
    }

    [Fact]
    public void BuildContainer_Transients_CreateNewInstances()
    {
        using var host = BuildHost();
        var provider = host.Services;

        var first1 = provider.GetRequiredService<SearchService>();
        var second1 = provider.GetRequiredService<SearchService>();
        Assert.NotSame(first1, second1);

        var first2 = provider.GetRequiredService<VideoService>();
        var second2 = provider.GetRequiredService<VideoService>();
        Assert.NotSame(first2, second2);

        var first3 = provider.GetRequiredService<DownloadService>();
        var second3 = provider.GetRequiredService<DownloadService>();
        Assert.NotSame(first3, second3);
    }

    [Fact]
    public void BuildContainer_NoCircularDependencies()
    {
        using var host = BuildHost();
        var provider = host.Services;

        provider.GetRequiredService<LocalizationService>();
        provider.GetRequiredService<ISettingsService>();
        provider.GetRequiredService<IAuthenticationService>();
        provider.GetRequiredService<IVideoProvider>();
        provider.GetRequiredService<IDownloadEngine>();
        provider.GetRequiredService<IDownloadRepository>();
        provider.GetRequiredService<IVideoDownloadProvider>();
        provider.GetRequiredService<IDownloadManager>();
        provider.GetRequiredService<IThumbnailCache>();
        provider.GetRequiredService<IHistoryService>();
        provider.GetRequiredService<IFavoritesService>();
        provider.GetRequiredService<IPlaylistService>();
        provider.GetRequiredService<IVKWebViewService>();
        provider.GetRequiredService<PlaybackService>();
        provider.GetRequiredService<ErrorHandlerService>();
        provider.GetRequiredService<DeepLinkService>();
        provider.GetRequiredService<SearchService>();
        provider.GetRequiredService<VideoService>();
        provider.GetRequiredService<DownloadService>();
        provider.GetRequiredService<NotificationService>();
        provider.GetRequiredService<AppMetrics>();
    }

    [Fact]
    public void BuildContainer_DownloadManager_HasAllDependencies()
    {
        using var host = BuildHost();
        var provider = host.Services;

        var manager = provider.GetRequiredService<IDownloadManager>();

        Assert.NotNull(manager);
        Assert.IsType<DownloadManager>(manager);
    }

    [Fact]
    public void BuildContainer_AllInterfaces_Registered()
    {
        using var host = BuildHost();
        var provider = host.Services;

        var interfaces = new Type[]
        {
            typeof(ISettingsService),
            typeof(IAuthenticationService),
            typeof(IVideoProvider),
            typeof(IDownloadEngine),
            typeof(IDownloadRepository),
            typeof(IVideoDownloadProvider),
            typeof(IDownloadManager),
            typeof(IThumbnailCache),
            typeof(IHistoryService),
            typeof(IFavoritesService),
            typeof(IPlaylistService),
            typeof(IVKWebViewService),
        };

        foreach (var iface in interfaces)
        {
            var service = provider.GetService(iface);
            Assert.NotNull(service);
        }
    }
}
