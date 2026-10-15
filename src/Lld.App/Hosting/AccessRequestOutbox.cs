using System.Text.Json;
using System.Text.Json.Serialization;
using Lld.App.Auth;
using Lld.Contracts;
using Lld.ServerStatus;

namespace Lld.App.Hosting;

/// <summary>What the person sees after asking for access: sent now, or saved until the server is back.</summary>
public sealed record AccessRequestOutcome(bool Queued);

/// <summary>
/// Store-and-forward for access requests (Outbox pattern, PRD F1/F8). A request made while the server is offline
/// is saved in a file on the device and sent when the status turns online. Resending is safe: the server merges
/// duplicate pending requests for the same email.
/// </summary>
public sealed class AccessRequestOutbox : IDisposable
{
    private const string FileName = "access-request-outbox.json";

    private readonly ApiClient _api;
    private readonly ServerStatusTracker _tracker;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AccessRequestOutbox(ApiClient api, ServerStatusTracker tracker, string appDataDirectory)
    {
        _api = api;
        _tracker = tracker;
        _path = Path.Combine(appDataDirectory, FileName);
        tracker.Changed += OnStatusChanged;
    }

    /// <summary>Raised for each queued request that reached the server (Observer: the UI shows a toast).</summary>
    public event EventHandler<AccessRequestRequest>? Sent;

    /// <summary>Sends now when the server may be reachable; otherwise, or on a network failure, saves it for later.</summary>
    public async Task<ApiResult<AccessRequestOutcome>> SubmitAsync(AccessRequestRequest request, CancellationToken ct)
    {
        if (_tracker.Current.State != ServerState.Offline)
        {
            var result = await _api.RequestAccessAsync(request, ct);
            if (result.Ok)
            {
                return new ApiResult<AccessRequestOutcome>(new AccessRequestOutcome(Queued: false), null, result.Status);
            }

            if (!IsUnreachable(result))
            {
                return new ApiResult<AccessRequestOutcome>(default, result.Error, result.Status); // e.g. invalid email
            }
        }

        await EnqueueAsync(request, ct);
        return new ApiResult<AccessRequestOutcome>(new AccessRequestOutcome(Queued: true), null);
    }

    /// <summary>Sends saved requests in order; stops at the first network failure and keeps the rest.</summary>
    public async Task FlushAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var pending = await ReadAsync(ct);
            while (pending.Count > 0)
            {
                var result = await _api.RequestAccessAsync(pending[0], ct);
                if (IsUnreachable(result))
                {
                    break;
                }

                var sent = pending[0];
                pending.RemoveAt(0);
                await WriteAsync(pending, ct);
                if (result.Ok)
                {
                    Sent?.Invoke(this, sent);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _tracker.Changed -= OnStatusChanged;
        _gate.Dispose();
    }

    private async Task EnqueueAsync(AccessRequestRequest request, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var pending = await ReadAsync(ct);
            pending.RemoveAll(r => string.Equals(r.Email.Trim(), request.Email.Trim(), StringComparison.OrdinalIgnoreCase));
            pending.Add(request); // the latest details for an email win, as on the server
            await WriteAsync(pending, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnStatusChanged(object? sender, ServerStatusSnapshot snapshot)
    {
        if (snapshot.IsOnline)
        {
            _ = FlushAsync(CancellationToken.None);
        }
    }

    private static bool IsUnreachable(ApiResult<AcceptedResponse> result) =>
        result.Error == BridgeError.Network || result.Status is 502 or 503 or 504;

    private async Task<List<AccessRequestRequest>> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync(stream, OutboxJsonContext.Default.ListAccessRequestRequest, ct) ?? [];
    }

    private async Task WriteAsync(List<AccessRequestRequest> pending, CancellationToken ct)
    {
        if (pending.Count == 0)
        {
            File.Delete(_path);
            return;
        }

        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, pending, OutboxJsonContext.Default.ListAccessRequestRequest, ct);
        }

        File.Move(temp, _path, overwrite: true); // never leave a half-written outbox
    }
}

[JsonSerializable(typeof(List<AccessRequestRequest>))]
internal sealed partial class OutboxJsonContext : JsonSerializerContext;
