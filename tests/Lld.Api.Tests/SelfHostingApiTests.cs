using System.Net;
using FluentAssertions;
using Lld.Api.Hosting;
using Lld.Application.Hosting;
using Lld.Infrastructure.Hosting;
using Lld.ServerStatus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lld.Api.Tests;

/// <summary>
/// The API as hosted: Kestrel on localhost (simulated: every request arrives from loopback) and the tunnel
/// agent adding X-Forwarded-For. Status announcements go to a recording fake instead of ntfy.sh.
/// </summary>
public class SelfHostingApiFactory : LldApiFactory
{
    public const int GlobalPerMinute = 3;
    public const int MonthlyRequests = 20;

    public RecordingAnnouncer Announcer { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:GlobalPerMinute", GlobalPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Budget:MonthlyRequests", MonthlyRequests.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Budget:HeaderAllowanceBytes", "0");
        builder.UseSetting("ServerStatus:Topic", "test-topic");
        builder.UseSetting("ServerStatus:PrivateKey", StatusKeyPair.Create().PrivateKeyPkcs8);
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter, LoopbackConnectionFilter>();
            services.AddSingleton<IStatusAnnouncer>(Announcer);
        });
    }

    /// <summary>A request as ngrok forwards it: from loopback, with the real client in X-Forwarded-For.</summary>
    public static HttpRequestMessage Tunnelled(string path, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", clientIp);
        request.Headers.Add("X-Forwarded-Proto", "https");
        return request;
    }

    public T HostedService<T>() => Services.GetServices<IHostedService>().OfType<T>().Single();

    private sealed class LoopbackConnectionFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}

public sealed class RecordingAnnouncer : IStatusAnnouncer
{
    private readonly List<StatusMessage> _messages = [];

    public bool IsConfigured => true;

    public IReadOnlyList<StatusMessage> Messages
    {
        get
        {
            lock (_messages)
            {
                return [.. _messages];
            }
        }
    }

    public Task AnnounceAsync(StatusMessage message, CancellationToken cancellationToken)
    {
        lock (_messages)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }
}

public sealed class TunnelTrafficTests : IDisposable
{
    private readonly SelfHostingApiFactory _factory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OnlyTunnelledRequests_CountAgainstTheBudget()
    {
        using var client = _factory.CreateClient();
        var meter = _factory.Services.GetRequiredService<RequestBudgetMeter>();
        var before = meter.Usage;

        (await client.GetAsync("/health", Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        meter.Usage.Requests.Should().Be(before.Requests, "local calls are free");

        using var tunnelled = SelfHostingApiFactory.Tunnelled("/health", "203.0.113.7");
        (await client.SendAsync(tunnelled, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);

        meter.Usage.Requests.Should().Be(before.Requests + 1);
        meter.Usage.Bytes.Should().BeGreaterThan(before.Bytes, "the response body is counted");
    }

    [Fact]
    public async Task GlobalRateLimit_AppliesPerRealClientIp_AndNotToLocalCalls()
    {
        using var client = _factory.CreateClient();

        for (var i = 0; i < SelfHostingApiFactory.GlobalPerMinute; i++)
        {
            using var allowed = SelfHostingApiFactory.Tunnelled("/health", "198.51.100.1");
            (await client.SendAsync(allowed, Ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var limited = SelfHostingApiFactory.Tunnelled("/health", "198.51.100.1");
        (await client.SendAsync(limited, Ct)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        using var otherClient = SelfHostingApiFactory.Tunnelled("/health", "198.51.100.2");
        (await client.SendAsync(otherClient, Ct)).StatusCode.Should().Be(HttpStatusCode.OK, "each client IP has its own window");

        for (var i = 0; i <= SelfHostingApiFactory.GlobalPerMinute; i++)
        {
            (await client.GetAsync("/health", Ct)).StatusCode.Should().Be(HttpStatusCode.OK, "loopback is not limited");
        }
    }

    [Fact]
    public async Task Flusher_SavesUsageSoItSurvivesARestart()
    {
        using var client = _factory.CreateClient();
        using var tunnelled = SelfHostingApiFactory.Tunnelled("/health", "203.0.113.9");
        await client.SendAsync(tunnelled, Ct);

        await _factory.HostedService<RequestBudgetFlusher>().FlushAsync(Ct);

        await using var scope = _factory.Services.CreateAsyncScope();
        var saved = await scope.ServiceProvider.GetRequiredService<GetBudgetUsage>().ExecuteAsync(Ct);
        saved.Requests.Should().BeGreaterThanOrEqualTo(1);
    }
}

public sealed class ServerStatusPublisherTests : IDisposable
{
    private readonly SelfHostingApiFactory _factory = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnnouncesOnlineAtStartup_ThenOnlyWhenSomethingChanges()
    {
        _ = _factory.Services; // starts the host
        var startup = await WaitForMessagesAsync(1);
        startup[0].Status.Should().Be(ServerState.Online);
        startup[0].Reason.Should().Be(StatusReasons.Startup);

        await _factory.HostedService<ServerStatusPublisher>().ReconcileAsync(StatusReasons.Heartbeat, Ct);

        _factory.Announcer.Messages.Should().HaveCount(1, "nothing changed and no heartbeat is due");
    }

    [Fact]
    public async Task AnnouncesOfflineForQuota_WhenTheBudgetIsNearlySpent()
    {
        _ = _factory.Services;
        await WaitForMessagesAsync(1);
        var meter = _factory.Services.GetRequiredService<RequestBudgetMeter>();
        meter.Load(SelfHostingApiFactory.MonthlyRequests, 0);

        await _factory.HostedService<ServerStatusPublisher>().ReconcileAsync(StatusReasons.Heartbeat, Ct);

        var last = _factory.Announcer.Messages[^1];
        last.Status.Should().Be(ServerState.Offline);
        last.Reason.Should().Be(StatusReasons.Quota);
        last.QuotaResetsAt.Should().Be(meter.Window.ResetsAt);
    }

    [Fact]
    public async Task AnnouncesOfflineOnGracefulStop()
    {
        _ = _factory.Services;
        await WaitForMessagesAsync(1);

        await _factory.HostedService<ServerStatusPublisher>().StopAsync(Ct);

        var last = _factory.Announcer.Messages[^1];
        last.Status.Should().Be(ServerState.Offline);
        last.Reason.Should().Be(StatusReasons.Shutdown);
    }

    private async Task<IReadOnlyList<StatusMessage>> WaitForMessagesAsync(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_factory.Announcer.Messages.Count < count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        return _factory.Announcer.Messages;
    }
}
