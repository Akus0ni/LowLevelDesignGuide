using Lld.App.Auth;
using Lld.Content;
using Lld.Contracts;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lld.App.Content;

/// <summary>
/// The bridge's content calls: wait for <see cref="ContentGate"/>, ask the device catalog, and turn "missing" or
/// "broken" into a <see cref="BridgeError"/> React can show. All reads are local, so they work offline.
/// </summary>
public sealed partial class ContentService(ContentGate gate, IContentCatalog catalog, ILogger<ContentService> logger)
{
    public static readonly BridgeError NotFound =
        new(ContentErrorCodes.NotFound, "This lesson isn't in this version of the app.");

    public static readonly BridgeError Unavailable =
        new(UnavailableCode, "Lessons couldn't be loaded on this device. Restart the app and try again.");

    public const string UnavailableCode = "content.unavailable";

    public Task<BridgeResult<IReadOnlyList<ModuleSummary>>> GetModulesAsync() =>
        RunAsync(ct => catalog.GetModulesAsync(ct)!);

    public Task<BridgeResult<ModuleDetail>> GetModuleAsync(string slug) =>
        RunAsync(ct => catalog.GetModuleAsync(slug, ct));

    public Task<BridgeResult<LessonDetail>> GetLessonAsync(string id) =>
        RunAsync(ct => catalog.GetLessonAsync(id, ct));

    public Task<BridgeResult<IReadOnlyList<CheatSheetSummary>>> GetCheatSheetsAsync(string? moduleSlug) =>
        RunAsync(ct => catalog.GetCheatSheetsAsync(moduleSlug, ct)!);

    public Task<BridgeResult<CheatSheetDetail>> GetCheatSheetAsync(string lessonId) =>
        RunAsync(ct => catalog.GetCheatSheetAsync(lessonId, ct));

    private async Task<BridgeResult<T>> RunAsync<T>(Func<CancellationToken, Task<T?>> query)
        where T : class
    {
        try
        {
            await gate.ReadyAsync().ConfigureAwait(false);
            var value = await query(CancellationToken.None).ConfigureAwait(false);
            return value is null ? BridgeResult.Failure<T>(NotFound) : BridgeResult.Success(value);
        }
        catch (Exception ex) when (ex is SqliteException or InvalidDataException or IOException or InvalidOperationException)
        {
            LogUnavailable(ex);
            return BridgeResult.Failure<T>(Unavailable);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Content is unavailable on this device")]
    private partial void LogUnavailable(Exception exception);
}
