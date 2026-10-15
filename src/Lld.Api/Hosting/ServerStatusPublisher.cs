using Lld.Application.Hosting;
using Lld.Infrastructure.Hosting;
using Lld.ServerStatus;
using Microsoft.Extensions.Options;

namespace Lld.Api.Hosting;

/// <summary>
/// Announces the server's status to every app through the signed ntfy topic (spec 002 §3.2).
/// A reconcile loop: each minute it works out what the status should be (online, or offline for quota) and
/// publishes when that differs from what was last published, or when a heartbeat is due. Off when not configured.
/// </summary>
internal sealed class ServerStatusPublisher(
    IStatusAnnouncer announcer,
    IOptions<ServerStatusOptions> options,
    RequestBudgetMeter budget,
    IHttpClientFactory httpClients,
    TimeProvider time,
    ILogger<ServerStatusPublisher> logger) : BackgroundService
{
    public const string PublicHealthClient = "public-health";
    public static readonly TimeSpan ReconcileInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ShutdownPublishTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FirstProbeDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxProbeDelay = TimeSpan.FromMinutes(1);

    private readonly ServerStatusOptions _options = options.Value;
    private StatusMessage? _lastPublished;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsConfigured)
        {
            logger.StatusPublishingOff();
            return;
        }

        try
        {
            await WaitUntilPubliclyReachableAsync(stoppingToken);
            await ReconcileAsync(StatusReasons.Startup, stoppingToken);

            using var timer = new PeriodicTimer(ReconcileInterval, time);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ReconcileAsync(StatusReasons.Heartbeat, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping: StopAsync announces "offline".
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (!_options.IsConfigured || _lastPublished is null)
        {
            return;
        }

        // Windows gives services only a few seconds at shutdown; apps also detect a dead server on their next call.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ShutdownPublishTimeout);
        await TryPublishAsync(StatusMessage.Create(ServerState.Offline, StatusReasons.Shutdown, time.GetUtcNow()), timeout.Token);
    }

    /// <summary>Publishes when the desired status changed or a heartbeat is due; otherwise does nothing.</summary>
    internal async Task ReconcileAsync(string reasonIfUnchanged, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var desired = Desired(now, reasonIfUnchanged);
        var changed = _lastPublished is null
            || _lastPublished.Status != desired.Status
            || _lastPublished.QuotaResetsAt != desired.QuotaResetsAt;
        var heartbeatDue = _lastPublished is not null && now - _lastPublished.At >= _options.HeartbeatInterval;

        if (changed || heartbeatDue)
        {
            await TryPublishAsync(desired, cancellationToken);
        }
    }

    private StatusMessage Desired(DateTimeOffset now, string reasonIfUnchanged)
    {
        var usage = budget.Usage;
        if (usage.IsNearLimit)
        {
            return StatusMessage.Create(ServerState.Offline, StatusReasons.Quota, now, usage.Window.ResetsAt);
        }

        var reason = _lastPublished is { Reason: StatusReasons.Quota } ? StatusReasons.QuotaReset : reasonIfUnchanged;
        return StatusMessage.Create(ServerState.Online, reason, now);
    }

    private async Task TryPublishAsync(StatusMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await announcer.AnnounceAsync(message, cancellationToken);
            _lastPublished = message;
            var (status, reason) = (message.Status, message.Reason);
            logger.StatusAnnounced(status, reason);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // ntfy.sh or the internet is down, or the shutdown timeout hit: try again on the next tick (if any).
            var status = message.Status;
            logger.StatusAnnounceFailed(ex, status);
        }
    }

    /// <summary>Waits until the tunnel forwards to this API, so "online" is never announced before apps can connect.</summary>
    private async Task WaitUntilPubliclyReachableAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicUrl))
        {
            return;
        }

        var healthUrl = new Uri($"{_options.PublicUrl.TrimEnd('/')}/health");
        var http = httpClients.CreateClient(PublicHealthClient);
        var delay = FirstProbeDelay;
        while (true)
        {
            try
            {
                using var response = await http.GetAsync(healthUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                var statusCode = response.StatusCode;
                logger.PublicUrlNotReady(statusCode);
            }
            catch (HttpRequestException ex)
            {
                logger.PublicUrlUnreachable(ex);
            }

            await Task.Delay(delay, time, cancellationToken);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxProbeDelay.Ticks));
        }
    }
}
