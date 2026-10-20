using System.Net;

namespace Lld.Samples.Patterns.Structural.Decorator.Modern;

/// <summary>
/// .NET's own decorator for HTTP: a <see cref="DelegatingHandler"/> wraps the next handler and adds behaviour. With
/// <c>IHttpClientFactory</c> you stack them with <c>AddHttpMessageHandler&lt;RetryHandler&gt;()</c>.
/// </summary>
public sealed class RetryHandler(int attempts = 3) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.ServiceUnavailable || attempt == attempts)
            {
                return response;
            }

            response.Dispose();
        }
    }
}
