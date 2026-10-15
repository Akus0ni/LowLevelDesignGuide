using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Lld.App.Hosting;

/// <summary>
/// Which server the app talks to and how it hears about the server's status (spec 002).
/// <list type="bullet">
/// <item><b>aspire</b> (Debug default): the dev API through Aspire service discovery; status is assumed online.</item>
/// <item><b>hosted</b> (Release default): <c>Api:BaseUrl</c> (the tunnel) and the signed ntfy status topic.</item>
/// </list>
/// Values are read by key (no reflection binding), so Release trimming cannot break them.
/// </summary>
public sealed record HostingSettings(bool UseHostedServer, Uri? BaseUrl, Uri NtfyBaseUrl, string? Topic, string? PublicKey)
{
    public const string HostedTarget = "hosted";
    public const string AspireTarget = "aspire";

    private const string CommittedSettings = "Lld.App.appsettings.json";
    private const string LocalSettings = "Lld.App.appsettings.local.json"; // git-ignored, embedded only if present

    /// <summary>Status comes from ntfy only when a topic and key are configured for the hosted server.</summary>
    public bool MonitorsStatus => UseHostedServer && !string.IsNullOrWhiteSpace(Topic) && !string.IsNullOrWhiteSpace(PublicKey);

    /// <summary>Adds the embedded settings files to the app's configuration.</summary>
    public static void AddTo(IConfigurationBuilder configuration)
    {
        var assembly = typeof(HostingSettings).Assembly;
        foreach (var name in new[] { CommittedSettings, LocalSettings })
        {
            if (assembly.GetManifestResourceStream(name) is { } stream)
            {
                configuration.AddJsonStream(stream);
            }
        }
    }

    public static HostingSettings From(IConfiguration configuration, bool isDebugBuild)
    {
        var target = configuration["Api:Target"];
        var hosted = string.IsNullOrWhiteSpace(target) ? !isDebugBuild : string.Equals(target, HostedTarget, StringComparison.OrdinalIgnoreCase);
        var baseUrl = Uri.TryCreate(configuration["Api:BaseUrl"], UriKind.Absolute, out var url) ? url : null;

        return new HostingSettings(
            UseHostedServer: hosted && baseUrl is not null,
            BaseUrl: baseUrl,
            NtfyBaseUrl: new Uri(configuration["ServerStatus:NtfyBaseUrl"] ?? "https://ntfy.sh"),
            Topic: configuration["ServerStatus:Topic"],
            PublicKey: configuration["ServerStatus:PublicKey"]);
    }
}
