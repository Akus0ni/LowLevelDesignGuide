using Lld.ServerStatus;

namespace Lld.Api.Hosting;

/// <summary>Source-generated log messages for self-hosting (no allocations when the level is off).</summary>
internal static partial class HostingLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not save tunnel usage ({Requests} requests); retrying on the next flush")]
    public static partial void UsageSaveFailed(this ILogger logger, Exception exception, long requests);

    [LoggerMessage(Level = LogLevel.Information, Message = "New tunnel budget window started: {WindowStart}")]
    public static partial void BudgetWindowStarted(this ILogger logger, DateTimeOffset windowStart);

    [LoggerMessage(Level = LogLevel.Information, Message = "Server status publishing is off (no ServerStatus:Topic / ServerStatus:PrivateKey)")]
    public static partial void StatusPublishingOff(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Announced server {Status} ({Reason})")]
    public static partial void StatusAnnounced(this ILogger logger, ServerState status, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not announce server {Status}; will retry")]
    public static partial void StatusAnnounceFailed(this ILogger logger, Exception exception, ServerState status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Public URL answered {StatusCode}; waiting for the tunnel")]
    public static partial void PublicUrlNotReady(this ILogger logger, System.Net.HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Information, Message = "Public URL not reachable yet; waiting for the tunnel")]
    public static partial void PublicUrlUnreachable(this ILogger logger, Exception exception);
}
