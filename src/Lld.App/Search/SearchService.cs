using Lld.App.Auth;
using Lld.App.Content;
using Lld.App.Data.Search;
using Lld.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lld.App.Search;

/// <summary>
/// The bridge's search call (spec 009 §3.8): wait for the device database (<see cref="ContentGate"/> runs the schema
/// steps and loads content), then search it with <see cref="DeviceSearch"/>. Local only, so it works offline.
/// </summary>
public sealed partial class SearchService(ContentGate gate, DeviceSearch search, ILogger<SearchService> logger)
{
    public static readonly BridgeError Unavailable =
        new("search.unavailable", "Search isn't available on this device right now. Restart the app and try again.");

    public async Task<BridgeResult<SearchResultsDto>> SearchAsync(SearchRequestDto request)
    {
        try
        {
            await gate.ReadyAsync().ConfigureAwait(false);
            return BridgeResult.Success(await search.SearchAsync(request, CancellationToken.None).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
        {
            LogUnavailable(ex);
            return BridgeResult.Failure<SearchResultsDto>(Unavailable);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Search is unavailable on this device")]
    private partial void LogUnavailable(Exception exception);
}
