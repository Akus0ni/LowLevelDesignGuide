namespace Lld.ServerStatus;

/// <summary>What the app currently believes about the server, and since when.</summary>
public sealed record ServerStatusSnapshot(ServerState State, string Reason, DateTimeOffset Since, DateTimeOffset? QuotaResetsAt = null)
{
    public bool IsOnline => State == ServerState.Online;

    /// <summary>The server asked apps to stop calling until its monthly request budget resets.</summary>
    public bool IsStoppedForQuota => State == ServerState.Offline && Reason == StatusReasons.Quota;
}

/// <summary>
/// Turns signed messages and the outcome of the app's own API calls into one status (a small state machine).
/// Rules (spec 002 §3.1):
/// <list type="bullet">
/// <item>A message is accepted only if it is newer than the last accepted one and not from the far future (anti-replay).</item>
/// <item>A failed call means Offline; a successful call means Online, except while offline for quota.</item>
/// <item>With no evidence for <see cref="StaleAfter"/>, the status becomes Unknown.</item>
/// </list>
/// Observers subscribe to <see cref="Changed"/>.
/// </summary>
public sealed class ServerStatusTracker
{
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan StaleAfter = TimeSpan.FromHours(7);

    private readonly TimeProvider _time;
    private readonly Lock _gate = new();
    private DateTimeOffset? _lastMessageAt;
    private DateTimeOffset? _lastEvidenceAt;

    public ServerStatusTracker(TimeProvider time, ServerState initialState = ServerState.Unknown)
    {
        _time = time;
        Current = new ServerStatusSnapshot(
            initialState,
            initialState == ServerState.Online ? StatusReasons.Assumed : StatusReasons.NoSignal,
            time.GetUtcNow());
    }

    public event EventHandler<ServerStatusSnapshot>? Changed;

    public ServerStatusSnapshot Current { get; private set; }

    /// <summary>Applies a verified message. Returns false when it is ignored as a replay or from the future.</summary>
    public bool Apply(StatusMessage message)
    {
        var now = _time.GetUtcNow();
        ServerStatusSnapshot? changed;
        lock (_gate)
        {
            if (message.At > now + MaxClockSkew || message.At <= _lastMessageAt)
            {
                return false;
            }

            _lastMessageAt = message.At;
            if (now - message.At > StaleAfter)
            {
                return true; // genuine but too old to describe the present
            }

            _lastEvidenceAt = Max(_lastEvidenceAt, message.At);
            var since = Current.State == message.Status ? Current.Since : message.At;
            changed = Transition(new ServerStatusSnapshot(message.Status, message.Reason, since, message.QuotaResetsAt));
        }

        Notify(changed);
        return true;
    }

    /// <summary>A network error or a gateway error (502/503/504) from the API.</summary>
    public void ReportCallFailed()
    {
        var now = _time.GetUtcNow();
        ServerStatusSnapshot? changed = null;
        lock (_gate)
        {
            _lastEvidenceAt = now;
            if (Current.State != ServerState.Offline)
            {
                changed = Transition(new ServerStatusSnapshot(ServerState.Offline, StatusReasons.Unreachable, now));
            }
        }

        Notify(changed);
    }

    /// <summary>The API answered. A quota stop stays in force: the server asked apps not to call.</summary>
    public void ReportCallSucceeded()
    {
        var now = _time.GetUtcNow();
        ServerStatusSnapshot? changed = null;
        lock (_gate)
        {
            _lastEvidenceAt = now;
            if (Current.State != ServerState.Online && !Current.IsStoppedForQuota)
            {
                changed = Transition(new ServerStatusSnapshot(ServerState.Online, StatusReasons.Reachable, now));
            }
        }

        Notify(changed);
    }

    /// <summary>Called periodically: with no recent evidence, the last known state can no longer be trusted.</summary>
    public void Evaluate()
    {
        var now = _time.GetUtcNow();
        ServerStatusSnapshot? changed = null;
        lock (_gate)
        {
            if (_lastEvidenceAt is { } last && now - last > StaleAfter && Current.State != ServerState.Unknown)
            {
                changed = Transition(new ServerStatusSnapshot(ServerState.Unknown, StatusReasons.Stale, now));
            }
        }

        Notify(changed);
    }

    /// <summary>Stores the new snapshot; returns it only if observers should hear about it (a heartbeat is not news).</summary>
    private ServerStatusSnapshot? Transition(ServerStatusSnapshot next)
    {
        var previous = Current;
        Current = next;
        var isNews = previous.State != next.State
            || previous.IsStoppedForQuota != next.IsStoppedForQuota
            || previous.QuotaResetsAt != next.QuotaResetsAt;
        return isNews ? next : null;
    }

    // Raised outside the lock so an observer can safely call back into the tracker.
    private void Notify(ServerStatusSnapshot? changed)
    {
        if (changed is not null)
        {
            Changed?.Invoke(this, changed);
        }
    }

    private static DateTimeOffset Max(DateTimeOffset? a, DateTimeOffset b) => a is { } value && value > b ? value : b;
}
