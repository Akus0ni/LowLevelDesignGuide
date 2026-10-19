using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Domain.Progress;
using Lld.Wire;

namespace Lld.App.Data.Progress;

/// <summary>Whose progress the device shows: the account whose session is stored here, or the guest.</summary>
public interface IProgressOwnerSource
{
    Task<ProgressOwner> GetOwnerAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The device's progress as the bridge needs it (spec 005 §3.10): wire records in and out, the owner resolved here
/// (never sent by JavaScript), this device's id stamped on every write, and the limits checked before saving.
/// </summary>
public sealed class DeviceProgress(SqliteProgressStore store, DeviceIdentity device, IProgressOwnerSource owners)
{
    public async Task<ProgressSnapshotDto> GetAsync(CancellationToken cancellationToken)
    {
        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        return ProgressWire.ToDto(await store.GetAsync(owner, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> SaveLessonsAsync(IReadOnlyList<LessonProgressDto> records, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);
        var parsed = ProgressWire.ToLessons(records);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var deviceId = await device.GetIdAsync(cancellationToken).ConfigureAwait(false);
        var lessons = parsed.Value.Select(lesson => lesson with { DeviceId = deviceId }).ToList();
        var valid = ProgressLimits.Validate(lessons);
        if (valid.IsFailure)
        {
            return valid;
        }

        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        await store.SaveLessonsAsync(owner, lessons, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result> RecordActivityAsync(string day, CancellationToken cancellationToken)
    {
        var parsed = ProgressWire.ToDay(day);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        await store.AddActivityDayAsync(owner, parsed.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>Hands the guest's progress to the user who just signed in (spec 005 §3.9); nothing happens when there is none.</summary>
    public Task AdoptGuestAsync(string userId, CancellationToken cancellationToken) =>
        store.AdoptGuestAsync(ProgressOwner.User(userId), cancellationToken);
}
