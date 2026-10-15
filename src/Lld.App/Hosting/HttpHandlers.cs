using System.Net;
using Lld.ServerStatus;

namespace Lld.App.Hosting;

/// <summary>
/// Adds the header that skips ngrok's browser warning page (PRD F8). Harmless for other tunnels.
/// One link of the HttpClient handler chain (Chain of Responsibility).
/// </summary>
public sealed class TunnelHeadersHandler : DelegatingHandler
{
    public const string NgrokSkipBrowserWarning = "ngrok-skip-browser-warning";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation(NgrokSkipBrowserWarning, "1");
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Reports every API call's outcome to the status tracker: a crash or power cut sends no "offline" message,
/// so a failed call is how the app notices (PRD F8). A gateway error means the tunnel is up but the API is not.
/// </summary>
public sealed class ServerReachabilityHandler(ServerStatusTracker tracker) : DelegatingHandler
{
    private static readonly HashSet<HttpStatusCode> GatewayErrors =
        [HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (GatewayErrors.Contains(response.StatusCode))
            {
                tracker.ReportCallFailed();
            }
            else
            {
                tracker.ReportCallSucceeded();
            }

            return response;
        }
        catch (HttpRequestException)
        {
            tracker.ReportCallFailed();
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            tracker.ReportCallFailed(); // timed out, not cancelled by the caller
            throw;
        }
    }
}
