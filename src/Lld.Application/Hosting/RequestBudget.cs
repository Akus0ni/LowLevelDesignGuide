namespace Lld.Application.Hosting;

/// <summary>
/// The tunnel's free monthly limits (ngrok: 20,000 requests and 1 GB) and when to stop short of them.
/// Bound from the <c>Budget</c> configuration section.
/// </summary>
public sealed class BudgetOptions
{
    public const string SectionName = "Budget";
    public const int MaxResetDay = 28;

    public long MonthlyRequests { get; set; } = 20_000;

    public long MonthlyBytes { get; set; } = 1L << 30;

    /// <summary>At this share of either limit the server announces itself offline (reason: quota).</summary>
    public double ThresholdPercent { get; set; } = 95;

    /// <summary>Day of the month (UTC) the tunnel provider resets usage; 1–28 so every month has it.</summary>
    public int ResetDayOfMonth { get; set; } = 1;

    /// <summary>Added per request for headers and TLS framing, which the app cannot see.</summary>
    public long HeaderAllowanceBytes { get; set; } = 800;
}

/// <summary>One billing window: from a reset day (inclusive) to the next one (exclusive), in UTC.</summary>
public readonly record struct BudgetWindow(DateTimeOffset Start, DateTimeOffset ResetsAt)
{
    /// <summary>Stable storage key, e.g. <c>2026-10-01</c>.</summary>
    public string Key => Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    public static BudgetWindow Containing(DateTimeOffset instant, int resetDay)
    {
        var day = Math.Clamp(resetDay, 1, BudgetOptions.MaxResetDay);
        var utc = instant.ToUniversalTime();
        var start = new DateTimeOffset(utc.Year, utc.Month, day, 0, 0, 0, TimeSpan.Zero);
        if (utc < start)
        {
            start = start.AddMonths(-1);
        }

        return new BudgetWindow(start, start.AddMonths(1));
    }
}

/// <summary>Usage in one window against the limits.</summary>
public sealed record BudgetUsage(BudgetWindow Window, long Requests, long Bytes, BudgetOptions Limits)
{
    public double RequestsPercent => Percent(Requests, Limits.MonthlyRequests);

    public double BytesPercent => Percent(Bytes, Limits.MonthlyBytes);

    /// <summary>True once either limit reaches the threshold: apps should stop calling until <see cref="BudgetWindow.ResetsAt"/>.</summary>
    public bool IsNearLimit => Math.Max(RequestsPercent, BytesPercent) >= Limits.ThresholdPercent;

    private static double Percent(long used, long limit) => limit <= 0 ? 0 : used * 100.0 / limit;
}

/// <summary>Port: persisted usage per window (survives restarts; read by the admin console).</summary>
public interface IUsageCounterStore
{
    Task AddAsync(string windowKey, long requests, long bytes, CancellationToken cancellationToken);

    Task<(long Requests, long Bytes)> GetAsync(string windowKey, CancellationToken cancellationToken);
}

/// <summary>
/// Counts tunnelled requests in memory (lock-free on the request path) and hands the not-yet-saved part to a
/// periodic flusher. Totals include what was loaded from storage at start-up.
/// </summary>
public sealed class RequestBudgetMeter(BudgetOptions options, TimeProvider time)
{
    private readonly Lock _gate = new();
    private BudgetWindow _window = BudgetWindow.Containing(time.GetUtcNow(), options.ResetDayOfMonth);
    private long _requests;
    private long _bytes;
    private long _pendingRequests;
    private long _pendingBytes;

    public BudgetOptions Options => options;

    public BudgetWindow Window
    {
        get
        {
            lock (_gate)
            {
                return _window;
            }
        }
    }

    public BudgetUsage Usage => new(Window, Interlocked.Read(ref _requests), Interlocked.Read(ref _bytes), options);

    /// <summary>Called once per tunnelled request with its body sizes; adds the header allowance.</summary>
    public void Record(long bodyBytes)
    {
        var bytes = bodyBytes + options.HeaderAllowanceBytes;
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _bytes, bytes);
        Interlocked.Increment(ref _pendingRequests);
        Interlocked.Add(ref _pendingBytes, bytes);
    }

    /// <summary>Seeds the totals for the current window from storage (start-up).</summary>
    public void Load(long requests, long bytes)
    {
        Interlocked.Add(ref _requests, requests);
        Interlocked.Add(ref _bytes, bytes);
    }

    /// <summary>Returns and clears the counts not yet saved, with the window they belong to.</summary>
    public (BudgetWindow Window, long Requests, long Bytes) TakePending()
    {
        lock (_gate)
        {
            return (_window, Interlocked.Exchange(ref _pendingRequests, 0), Interlocked.Exchange(ref _pendingBytes, 0));
        }
    }

    /// <summary>Puts counts back after a failed save so the next flush retries them (totals are unchanged).</summary>
    public void Requeue(long requests, long bytes)
    {
        Interlocked.Add(ref _pendingRequests, requests);
        Interlocked.Add(ref _pendingBytes, bytes);
    }

    /// <summary>Starts a new window when the reset day has passed. Call after flushing the pending counts.</summary>
    public bool RollIfNewWindow()
    {
        var current = BudgetWindow.Containing(time.GetUtcNow(), options.ResetDayOfMonth);
        lock (_gate)
        {
            if (current == _window)
            {
                return false;
            }

            _window = current;
            Interlocked.Exchange(ref _requests, 0);
            Interlocked.Exchange(ref _bytes, 0);
            return true;
        }
    }
}

/// <summary>Use case: this window's usage as last saved (the admin console runs in another process).</summary>
public sealed class GetBudgetUsage(IUsageCounterStore store, BudgetOptions options, TimeProvider time)
{
    public async Task<BudgetUsage> ExecuteAsync(CancellationToken cancellationToken)
    {
        var window = BudgetWindow.Containing(time.GetUtcNow(), options.ResetDayOfMonth);
        var (requests, bytes) = await store.GetAsync(window.Key, cancellationToken);
        return new BudgetUsage(window, requests, bytes, options);
    }
}
