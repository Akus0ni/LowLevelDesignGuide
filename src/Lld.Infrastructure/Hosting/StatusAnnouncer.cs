using System.Text;
using Lld.ServerStatus;
using Microsoft.Extensions.Options;

namespace Lld.Infrastructure.Hosting;

/// <summary>Port: tells every app the server's status, without the apps calling the server.</summary>
public interface IStatusAnnouncer
{
    bool IsConfigured { get; }

    Task AnnounceAsync(StatusMessage message, CancellationToken cancellationToken);
}

/// <summary>Signs the message and publishes it to the ntfy.sh topic. Costs no tunnel quota: apps read ntfy directly.</summary>
public sealed class NtfyStatusAnnouncer(HttpClient http, IOptions<ServerStatusOptions> options) : IStatusAnnouncer
{
    private readonly ServerStatusOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task AnnounceAsync(StatusMessage message, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Server status publishing is not configured (ServerStatus:Topic and ServerStatus:PrivateKey).");
        }

        using var signer = StatusSigner.FromPkcs8(_options.PrivateKey!);
        var body = signer.Sign(message);
        var url = $"{_options.NtfyBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(_options.Topic!)}";

        using var content = new StringContent(body, Encoding.UTF8, "text/plain");
        using var response = await http.PostAsync(new Uri(url), content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
