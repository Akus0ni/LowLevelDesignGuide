using System.Net;
using System.Threading.RateLimiting;
using Lld.Application.Hosting;
using Lld.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;

namespace Lld.Api.Hosting;

/// <summary>Everything the API needs to run on the owner's PC behind a tunnel (spec 002).</summary>
internal static class SelfHostingSetup
{
    public const int DefaultGlobalRequestsPerMinute = 60;
    private const string TunnelHeaderName = "ngrok-skip-browser-warning";

    public static IServiceCollection AddLldSelfHosting(this IServiceCollection services, IConfiguration configuration)
    {
        // Runs as a Windows service when started by the Service Control Manager; a no-op otherwise.
        services.AddWindowsService(options => options.ServiceName = "LldGuideApi");

        services.AddLldHosting(configuration);
        services.AddSingleton<RequestBudgetMeter>();
        services.AddSingleton<TunnelMetrics>();
        services.AddHostedService<RequestBudgetFlusher>();
        services.AddHostedService<ServerStatusPublisher>();
        services.AddHttpClient(ServerStatusPublisher.PublicHealthClient, http =>
            http.DefaultRequestHeaders.Add(TunnelHeaderName, "1"));

        // The tunnel agent runs on this PC: trust X-Forwarded-* from loopback only (the default known proxies).
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        var perMinute = configuration.GetValue("RateLimiting:GlobalPerMinute", DefaultGlobalRequestsPerMinute);
        services.Configure<RateLimiterOptions>(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                TunnelTraffic.IsTunnelled(context)
                    ? RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress!.ToString(),
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = perMinute, Window = TimeSpan.FromMinutes(1) })
                    : RateLimitPartition.GetNoLimiter(IPAddress.Loopback.ToString()));
        });

        services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddMeter(TunnelMetrics.MeterName));
        return services;
    }

    /// <summary>Must run first so every later component sees the real client address and scheme.</summary>
    public static IApplicationBuilder UseLldSelfHosting(this IApplicationBuilder app) =>
        app.UseForwardedHeaders()
           .UseMiddleware<RequestBudgetMiddleware>();
}
