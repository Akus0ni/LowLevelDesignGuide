using System.Text.Json;
using System.Text.Json.Serialization;
using Lld.App.Auth;
using Lld.App.Content;
using Lld.App.Data.Notes;
using Lld.Contracts;
using Lld.Domain.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lld.App.Notes;

/// <summary>
/// The bridge's notes and bookmarks calls (spec 007): wait for the device database (<see cref="ContentGate"/> runs the
/// schema steps), then use <see cref="DeviceNotes"/>. All local, so they work offline. Failures become a <see cref="BridgeError"/>.
/// </summary>
public sealed partial class NotesService(ContentGate gate, DeviceNotes notes, ILogger<NotesService> logger)
{
    public static readonly BridgeError Unavailable =
        new("notes.unavailable", "Your notes couldn't be saved on this device. Restart the app and try again.");

    private static readonly BridgeError BadRequest = new("validation.notes.json", "The notes were not readable.");

    public Task<BridgeResult<NotesSnapshotDto>> GetAsync() =>
        RunAsync(async ct => Result.Success(await notes.GetAsync(ct).ConfigureAwait(false)));

    /// <param name="recordsJson">A JSON array of <see cref="NoteDto"/> (camelCase), as React sends it.</param>
    public Task<BridgeResult<bool>> SaveNotesAsync(string recordsJson) =>
        Read(recordsJson, NotesBridgeJson.Default.NoteDtoArray) is { } records
            ? RunAsync(async ct => Done(await notes.SaveNotesAsync(records, ct).ConfigureAwait(false)))
            : Task.FromResult(BridgeResult.Failure<bool>(BadRequest));

    /// <param name="recordsJson">A JSON array of <see cref="BookmarkDto"/> (camelCase).</param>
    public Task<BridgeResult<bool>> SaveBookmarksAsync(string recordsJson) =>
        Read(recordsJson, NotesBridgeJson.Default.BookmarkDtoArray) is { } records
            ? RunAsync(async ct => Done(await notes.SaveBookmarksAsync(records, ct).ConfigureAwait(false)))
            : Task.FromResult(BridgeResult.Failure<bool>(BadRequest));

    /// <summary>Called after a sign-in: a failure is logged, never shown, because the sign-in itself worked.</summary>
    public async Task AdoptGuestAsync(string userId)
    {
        var adopted = await RunAsync(async ct => Done(await notes.AdoptGuestAsync(userId, ct).ConfigureAwait(false))).ConfigureAwait(false);
        if (!adopted.Ok)
        {
            LogAdoptionFailed(userId, adopted.Error?.Code);
        }
    }

    private static T[]? Read<T>(string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T[]> type)
    {
        try
        {
            return JsonSerializer.Deserialize(json, type);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Result<bool> Done(Result result) => result.IsSuccess ? true : result.Error!;

    private async Task<BridgeResult<T>> RunAsync<T>(Func<CancellationToken, Task<Result<T>>> work)
    {
        try
        {
            await gate.ReadyAsync().ConfigureAwait(false);
            var result = await work(CancellationToken.None).ConfigureAwait(false);
            return result.IsSuccess
                ? BridgeResult.Success(result.Value)
                : BridgeResult.Failure<T>(new BridgeError(result.Error!.Code, result.Error.Message));
        }
        catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
        {
            LogUnavailable(ex);
            return BridgeResult.Failure<T>(Unavailable);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Notes are unavailable on this device")]
    private partial void LogUnavailable(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Guest notes could not be adopted by user {UserId} ({Code})")]
    private partial void LogAdoptionFailed(string userId, string? code);
}

/// <summary>Source-generated, so it survives Android trimming; camelCase like the rest of the wire.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(NoteDto[]))]
[JsonSerializable(typeof(BookmarkDto[]))]
internal sealed partial class NotesBridgeJson : JsonSerializerContext;
