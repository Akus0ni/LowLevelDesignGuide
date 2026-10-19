using CommunityToolkit.Maui;
using Lld.App.Auth;
using Lld.App.Content;
using Lld.App.Data;
using Lld.App.Data.Content;
using Lld.App.Data.Progress;
using Lld.App.Data.Schema;
using Lld.App.Hosting;
using Lld.App.Progress;
using Lld.Content;
using Lld.ServerStatus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lld.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        HostingSettings.AddTo(builder.Configuration);
        builder.AddServiceDefaults();

#if DEBUG
        const bool isDebugBuild = true;
#else
        const bool isDebugBuild = false;
#endif
        var hosting = HostingSettings.From(builder.Configuration, isDebugBuild);
        builder.Services.AddSingleton(hosting);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(sp => new ServerStatusTracker(
            sp.GetRequiredService<TimeProvider>(),
            hosting.MonitorsStatus ? ServerState.Unknown : ServerState.Online));
        builder.Services.AddSingleton(Connectivity.Current);
        builder.Services.AddSingleton(sp => new ReachabilityRecovery(
            sp.GetRequiredService<ServerStatusTracker>(),
            ct => sp.GetRequiredService<ApiClient>().PingAsync(ct),
            sp.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton<ServerStatusMonitor>();
        builder.Services.AddTransient<TunnelHeadersHandler>();
        builder.Services.AddTransient<ServerReachabilityHandler>();

        builder.Services.AddSingleton(AppInfo.Current);
        builder.Services.AddSingleton(DeviceInfo.Current);
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        // Base URL (Strategy): the tunnel when hosted, Aspire service discovery in development.
        builder.Services.AddHttpClient<ApiClient>(http =>
                http.BaseAddress = hosting.UseHostedServer ? hosting.BaseUrl : new Uri(ApiClient.ServiceAddress))
            .AddHttpMessageHandler<TunnelHeadersHandler>()
            .AddHttpMessageHandler<ServerReachabilityHandler>();
        builder.Services.AddSingleton(sp => new AccessRequestOutbox(
            sp.GetRequiredService<ApiClient>(), sp.GetRequiredService<ServerStatusTracker>(), FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<SessionService>();

        // Content (spec 003): bundled pack → device SQLite → bridge. Plain SQLite, no EF Core (plan §4, T1).
        builder.Services.AddSingleton(FileSystem.Current);
        builder.Services.AddSingleton(Browser.Default);
        builder.Services.AddSingleton(new DeviceDatabase(Path.Combine(FileSystem.AppDataDirectory, DeviceDatabase.FileName)));
        builder.Services.AddSingleton(sp => new SchemaMigrator(sp.GetRequiredService<DeviceDatabase>(), SchemaMigrator.All));
        builder.Services.AddSingleton<ContentPackLoader>();
        builder.Services.AddSingleton<IContentPackSource, MauiContentPackSource>();
        builder.Services.AddSingleton<IContentCatalog, SqliteContentCatalog>();
        builder.Services.AddSingleton<ContentGate>();
        builder.Services.AddSingleton<ContentService>();

        // Progress (spec 005): same device database, owned by the stored session's user or the guest.
        builder.Services.AddSingleton<SqliteProgressStore>();
        builder.Services.AddSingleton<DeviceIdentity>();
        builder.Services.AddSingleton<IProgressOwnerSource, SessionProgressOwner>();
        builder.Services.AddSingleton<DeviceProgress>();
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton<BridgeService>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Services.AddHybridWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
