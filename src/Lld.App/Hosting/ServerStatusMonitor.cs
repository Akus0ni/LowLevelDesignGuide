using System.Text.Json;
using Lld.ServerStatus;
using Microsoft.Extensions.Logging;

namespace Lld.App.Hosting;

/// <summary>
/// Listens to the server's signed status on ntfy.sh (no tunnel quota is used) and feeds it to the tracker.
/// <list type="bullet">
/// <item>On start and resume: reads the topic's cached messages (ntfy keeps 12 h) and applies every valid one.</item>
/// <item>While in the foreground: keeps one streaming subscription open, reconnecting with back-off.</item>
/// <item>Every minute: lets the tracker downgrade old news to Unknown (local, no network).</item>
/// <item>When this app had concluded "unreachable", it re-checks once the connection may be back
/// (internet regained, stream reconnected, app resumed): see <see cref="ReachabilityRecovery"/>.</item>
/// </list>
/// Strategy: when the hosted server isn't used (Debug / Aspire), status is assumed online and only failed
/// API calls change it.
/// </summary>
public sealed partial class ServerStatusMonitor : IDisposable
{
    public static readonly TimeSpan EvaluateInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FirstReconnectDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromMinutes(1);
    private const string CacheWindow = "12h";
    private const string MessageEvent = "message";

    private readonly HostingSettings _settings;
    private readonly TimeProvider _time;
    private readonly ILogger<ServerStatusMonitor> _logger;
    private readonly HttpClient _http;
    private readonly StatusVerifier? _verifier;
    private readonly ReachabilityRecovery _recovery;
    private readonly IConnectivity _connectivity;
    private readonly Lock _gate = new();
    private CancellationTokenSource? _running;

    public ServerStatusMonitor(
        HostingSettings settings,
        ServerStatusTracker tracker,
        ReachabilityRecovery recovery,
        IConnectivity connectivity,
        TimeProvider time,
        ILogger<ServerStatusMonitor> logger)
    {
        _settings = settings;
        _recovery = recovery;
        _connectivity = connectivity;
        _time = time;
        _logger = logger;
        Tracker = tracker;

        // Its own client: the app's default resilience handler would cut a long-lived stream with request timeouts.
        _http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _verifier = settings.MonitorsStatus ? StatusVerifier.FromSubjectPublicKeyInfo(settings.PublicKey!) : null;
    }

    public ServerStatusTracker Tracker { get; }

    /// <summary>Starts (or restarts after resume) the poll + stream + evaluation loops. Safe to call repeatedly.</summary>
    public void Start()
    {
        CancellationToken token;
        lock (_gate)
        {
            if (_running is not null)
            {
                return;
            }

            _running = new CancellationTokenSource();
            token = _running.Token;
        }

        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        _ = EvaluateLoopAsync(token);
        _ = _recovery.TryRecoverAsync(token); // resumed while marked unreachable
        if (_verifier is not null)
        {
            _ = ListenAsync(token);
        }
    }

    /// <summary>Closes the stream when the app goes to the background (no work, no battery, no data).</summary>
    public void Stop()
    {
        CancellationTokenSource? running;
        lock (_gate)
        {
            running = _running;
            _running = null;
        }

        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        running?.Cancel();
        running?.Dispose();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _ = _recovery.TryRecoverAsync(CancellationToken.None); // e.g. mobile data switched back on
        }
    }

    public void Dispose()
    {
        Stop();
        _http.Dispose();
        _verifier?.Dispose();
    }

    private async Task ListenAsync(CancellationToken token)
    {
        var delay = FirstReconnectDelay;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await PollAsync(token);
                await _recovery.TryRecoverAsync(token); // ntfy answers again, so the connection is back
                await StreamAsync(token);
                delay = FirstReconnectDelay; // the stream ended normally: reconnect promptly
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException)
            {
                LogStatusSourceUnavailable(_logger, ex);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReconnectDelay.Ticks));
            }

            try
            {
                await Task.Delay(delay, _time, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>Reads what ntfy has cached, so a freshly opened app knows the status straight away.</summary>
    private async Task PollAsync(CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(PollTimeout);
        var body = await _http.GetStringAsync(TopicUri($"json?poll=1&since={CacheWindow}"), timeout.Token);
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Apply(line);
        }
    }

    /// <summary>One message per line (ntfy JSON stream); keepalive and open events are skipped.</summary>
    private async Task StreamAsync(CancellationToken token)
    {
        using var response = await _http.GetAsync(TopicUri("json"), HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(token) is { } line)
        {
            Apply(line);
        }
    }

    private void Apply(string ntfyLine)
    {
        try
        {
            using var json = JsonDocument.Parse(ntfyLine);
            var root = json.RootElement;
            if (root.GetProperty("event").GetString() != MessageEvent || !root.TryGetProperty("message", out var message))
            {
                return;
            }

            var verification = _verifier!.Verify(message.GetString() ?? string.Empty);
            if (verification.IsValid)
            {
                Tracker.Apply(verification.Message!);
            }
            else
            {
                LogMessageRejected(_logger, verification.Rejection!.Value);
            }
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            LogMessageRejected(_logger, StatusRejection.Malformed);
        }
    }

    private async Task EvaluateLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(EvaluateInterval, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                Tracker.Evaluate();
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }
    }

    private Uri TopicUri(string suffix) => new(_settings.NtfyBaseUrl, $"{Uri.EscapeDataString(_settings.Topic!)}/{suffix}");

    [LoggerMessage(Level = LogLevel.Information, Message = "Status source unavailable; retrying")]
    private static partial void LogStatusSourceUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ignored a status message: {Rejection}")]
    private static partial void LogMessageRejected(ILogger logger, StatusRejection rejection);
}
