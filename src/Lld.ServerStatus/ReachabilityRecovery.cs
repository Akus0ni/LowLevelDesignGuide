namespace Lld.ServerStatus;

/// <summary>
/// "Unreachable" is the app's own conclusion (one of its calls failed), not news from the server. If the server
/// never went down (the phone only lost its connection), no signed message will ever say "online" again, so the
/// app re-checks with one cheap call when it has a reason to think the connection is back: the device reports
/// internet again, the status stream reconnects, or the app is resumed. Never a timer: no polling.
/// The probe's outcome reaches the tracker through the app's HTTP pipeline (success → online).
/// </summary>
public sealed class ReachabilityRecovery(ServerStatusTracker tracker, Func<CancellationToken, Task> probe, TimeProvider time)
{
    /// <summary>A flapping connection triggers at most one probe per interval.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);

    private readonly Lock _gate = new();
    private DateTimeOffset? _lastProbeAt;
    private bool _probing;

    /// <summary>Only an app-inferred outage is re-checked; a signed "offline" (shutdown, quota) is the server's word.</summary>
    public bool IsNeeded => tracker.Current is { State: ServerState.Offline, Reason: StatusReasons.Unreachable };

    /// <summary>Returns true when a probe was made.</summary>
    public async Task<bool> TryRecoverAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            if (!IsNeeded || _probing || (_lastProbeAt is { } last && now - last < MinInterval))
            {
                return false;
            }

            _probing = true;
            _lastProbeAt = now;
        }

        try
        {
            await probe(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            // Still unreachable: the HTTP pipeline has already reported the failure.
        }
        finally
        {
            lock (_gate)
            {
                _probing = false;
            }
        }

        return true;
    }
}
