using System.Text.Json.Serialization;

namespace Lld.ServerStatus;

/// <summary>What the server says about itself. Unknown is never published; apps derive it locally.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ServerState>))]
public enum ServerState
{
    [JsonStringEnumMemberName("unknown")]
    Unknown,

    [JsonStringEnumMemberName("online")]
    Online,

    [JsonStringEnumMemberName("offline")]
    Offline,
}

/// <summary>Why the status changed. Published reasons come from the PC; the rest are set by the app.</summary>
public static class StatusReasons
{
    public const string Startup = "startup";
    public const string Heartbeat = "heartbeat";
    public const string Shutdown = "shutdown";
    public const string Quota = "quota";
    public const string QuotaReset = "quota_reset";
    public const string Manual = "manual";

    // Set on the device, never published.
    public const string NoSignal = "no_signal";
    public const string Stale = "stale";
    public const string Unreachable = "unreachable";
    public const string Reachable = "reachable";
    public const string Assumed = "assumed";
}

/// <summary>The signed payload published to the ntfy topic. <see cref="At"/> orders messages and defeats replays.</summary>
public sealed record StatusMessage(
    int V,
    ServerState Status,
    string Reason,
    DateTimeOffset At,
    DateTimeOffset? QuotaResetsAt = null)
{
    public const int CurrentVersion = 1;

    public static StatusMessage Create(ServerState status, string reason, DateTimeOffset at, DateTimeOffset? quotaResetsAt = null) =>
        new(CurrentVersion, status, reason, at, quotaResetsAt);
}

/// <summary>What goes over the wire: the payload bytes exactly as signed, plus the signature (both base64url).</summary>
public sealed record SignedEnvelope(string Payload, string Sig);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StatusMessage))]
[JsonSerializable(typeof(SignedEnvelope))]
internal sealed partial class StatusJsonContext : JsonSerializerContext;
