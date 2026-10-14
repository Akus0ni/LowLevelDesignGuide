using CommunityToolkit.Maui;
using Lld.App.Auth;
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

        builder.AddServiceDefaults();

        builder.Services.AddSingleton(AppInfo.Current);
        builder.Services.AddSingleton(DeviceInfo.Current);
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddHttpClient<ApiClient>(http => http.BaseAddress = new Uri(ApiClient.ServiceAddress));
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<BridgeService>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Services.AddHybridWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
