using System.Text.Json;
using System.Text.Json.Serialization;
using Lld.App.Auth;
using Lld.App.Content;
using Lld.App.Data.Progress;
using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Lld.App.Progress;

/// <summary>The account whose session is stored on this device owns the progress; with no session, the guest does.</summary>
public sealed class SessionProgressOwner(ITokenStore tokens) : IProgressOwnerSource
{
    public async Task<ProgressOwner> GetOwnerAsync(CancellationToken cancellationToken) =>
        await tokens.GetUserIdAsync().ConfigureAwait(false) is { Length: > 0 } userId ? ProgressOwner.User(userId) : ProgressOwner.Guest;
}

/// <summary>
/// The bridge's progress calls (spec 005): wait for the device database (<see cref="ContentGate"/> runs the schema steps),
/// then use <see cref="DeviceProgress"/>. All local, so they work offline. Failures become a <see cref="BridgeError"/>.
/// </summary>
public sealed partial class ProgressService(ContentGate gate, DeviceProgress progress, ILogger<ProgressService> logger)
{
    public static readonly BridgeError Unavailable =
        new("progress.unavailable", "Your progress couldn't be saved on this device. Restart the app and try again.");

    private static readonly BridgeError BadRequest = new("validation.progress.json", "The progress records were not readable.");

    public Task<BridgeResult<ProgressSnapshotDto>> GetAsync() =>
        RunAsync(async ct => Result.Success(await progress.GetAsync(ct).ConfigureAwait(false)));

    /// <param name="recordsJson">A JSON array of <see cref="LessonProgressDto"/> (camelCase), as React sends it.</param>
    public Task<BridgeResult<bool>> SaveLessonsAsync(string recordsJson)
    {
        LessonProgressDto[]? records;
        try
        {
            records = JsonSerializer.Deserialize(recordsJson, ProgressBridgeJson.Default.LessonProgressDtoArray);
        }
        catch (JsonException)
        {
            records = null;
        }

        return records is null
            ? Task.FromResult(BridgeResult.Failure<bool>(BadRequest))
            : RunAsync(async ct => Done(await progress.SaveLessonsAsync(records, ct).ConfigureAwait(false)));
    }

    public Task<BridgeResult<bool>> RecordActivityAsync(string day) =>
        RunAsync(async ct => Done(await progress.RecordActivityAsync(day, ct).ConfigureAwait(false)));

    /// <summary>Called after a sign-in: a failure is logged, never shown, because the sign-in itself worked.</summary>
    public async Task AdoptGuestAsync(string userId)
    {
        var adopted = await RunAsync(async ct =>
        {
            await progress.AdoptGuestAsync(userId, ct).ConfigureAwait(false);
            return Result.Success(true);
        }).ConfigureAwait(false);
        if (!adopted.Ok)
        {
            LogAdoptionFailed(userId);
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Progress is unavailable on this device")]
    private partial void LogUnavailable(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Guest progress could not be adopted by user {UserId}")]
    private partial void LogAdoptionFailed(string userId);
}

/// <summary>Source-generated, so it survives Android trimming; camelCase like the rest of the wire.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(LessonProgressDto[]))]
internal sealed partial class ProgressBridgeJson : JsonSerializerContext;
