using System.Diagnostics.Metrics;
using System.Net;

namespace Lld.Api.Hosting;

/// <summary>
/// Kestrel listens on localhost only when hosted, so a non-loopback client address (restored from
/// X-Forwarded-For by the forwarded-headers middleware) means the request came through the tunnel.
/// </summary>
internal static class TunnelTraffic
{
    public static bool IsTunnelled(HttpContext context) =>
        context.Connection.RemoteIpAddress is { } address && !IPAddress.IsLoopback(address);
}

/// <summary>OpenTelemetry view of the tunnel budget (visible in the Aspire dashboard during development).</summary>
internal sealed class TunnelMetrics
{
    public const string MeterName = "Lld.Api.Tunnel";

    private readonly Counter<long> _requests;
    private readonly Counter<long> _bytes;

    public TunnelMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _requests = meter.CreateCounter<long>("lld.tunnel.requests", unit: "{request}", description: "Requests that came through the tunnel");
        _bytes = meter.CreateCounter<long>("lld.tunnel.bytes", unit: "By", description: "Request + response body bytes through the tunnel");
    }

    public void Record(long bytes)
    {
        _requests.Add(1);
        _bytes.Add(bytes);
    }
}
