using Lld.Application.Hosting;

namespace Lld.Api.Hosting;

/// <summary>
/// Loads this window's saved usage at start-up, then saves the meter's new counts every minute and on stop,
/// so a restart never forgets usage and the admin console can read it.
/// </summary>
internal sealed class RequestBudgetFlusher(
    RequestBudgetMeter meter,
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<RequestBudgetFlusher> logger) : BackgroundService
{
    public static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(1);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await using (var scope = scopes.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IUsageCounterStore>();
            var (requests, bytes) = await store.GetAsync(meter.Window.Key, cancellationToken);
            meter.Load(requests, bytes);
        }

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping: StopAsync does the final flush.
        }
    }

    internal async Task FlushAsync(CancellationToken cancellationToken)
    {
        var (window, requests, bytes) = meter.TakePending();
        if (requests > 0 || bytes > 0)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IUsageCounterStore>().AddAsync(window.Key, requests, bytes, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.UsageSaveFailed(ex, requests);
                meter.Requeue(requests, bytes);
                return;
            }
        }

        if (meter.RollIfNewWindow())
        {
            var started = meter.Window.Start;
            logger.BudgetWindowStarted(started);
        }
    }
}
