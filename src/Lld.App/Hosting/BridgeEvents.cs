using System.Text.Json;
using System.Text.Json.Serialization;
using Lld.ServerStatus;

namespace Lld.App.Hosting;

/// <summary>The server status as React sees it (mirrors <c>ServerStatus</c> in web/src/data/types.ts).</summary>
public sealed record ServerStatusView(string State, string Reason, DateTimeOffset Since, DateTimeOffset? QuotaResetsAt)
{
    public static ServerStatusView From(ServerStatusSnapshot snapshot) => new(
        snapshot.State switch
        {
            ServerState.Online => "online",
            ServerState.Offline => "offline",
            _ => "unknown",
        },
        snapshot.Reason,
        snapshot.Since,
        snapshot.QuotaResetsAt);
}

/// <summary>Messages C# pushes to React with <c>HybridWebView.SendRawMessage</c>; React routes them by <see cref="Type"/>.</summary>
public sealed record BridgeEvent(string Type, ServerStatusView? Status = null, string? Email = null)
{
    public const string ServerStatusType = "serverStatus";
    public const string AccessRequestSentType = "accessRequestSent";

    public static BridgeEvent ForStatus(ServerStatusSnapshot snapshot) => new(ServerStatusType, ServerStatusView.From(snapshot));

    public static BridgeEvent ForAccessRequestSent(string email) => new(AccessRequestSentType, Email: email);

    public string ToJson() => JsonSerializer.Serialize(this, BridgeEventJsonContext.Default.BridgeEvent);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(BridgeEvent))]
internal sealed partial class BridgeEventJsonContext : JsonSerializerContext;
