using Lld.Application.Common;
using Lld.Domain.Common;
using Lld.Domain.Progress;

namespace Lld.Application.Progress;

// Progress use cases for the API (spec 005 §3.10). Every one acts on the signed-in user only: the user id comes from
// the access token, never from the request.

public sealed class GetProgress(IProgressStore store)
{
    public Task<ProgressSnapshot> ExecuteAsync(string userId, CancellationToken cancellationToken) =>
        store.GetAsync(ProgressOwner.User(userId), cancellationToken);
}

/// <summary>Saves lesson records (a reading change, a mark, or a reset); per lesson the later edit wins.</summary>
public sealed class SaveLessonProgress(IProgressStore store)
{
    public async Task<Result> ExecuteAsync(string userId, IReadOnlyCollection<LessonProgress> lessons, CancellationToken cancellationToken)
    {
        var valid = ProgressLimits.Validate(lessons);
        if (valid.IsFailure)
        {
            return valid;
        }

        await store.SaveLessonsAsync(ProgressOwner.User(userId), lessons, cancellationToken);
        return Result.Success();
    }
}

public sealed class RecordActivityDay(IProgressStore store, ActivityDayPolicy days)
{
    public async Task<Result> ExecuteAsync(string userId, DateOnly day, CancellationToken cancellationToken)
    {
        var valid = days.Validate([day]);
        if (valid.IsFailure)
        {
            return valid;
        }

        await store.AddActivityDayAsync(ProgressOwner.User(userId), day, cancellationToken);
        return Result.Success();
    }
}

/// <summary>Merges what a browser guest read into the user who just signed in (spec 005 §3.9).</summary>
public sealed class AdoptGuestProgress(IProgressStore store, ActivityDayPolicy days)
{
    public async Task<Result<ProgressSnapshot>> ExecuteAsync(string userId, ProgressSnapshot guest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(guest);
        var valid = ProgressLimits.Validate(guest.Lessons);
        if (valid.IsSuccess)
        {
            valid = days.Validate(guest.ActivityDays);
        }

        return valid.IsFailure
            ? valid.Error!
            : await store.MergeAsync(ProgressOwner.User(userId), guest, cancellationToken);
    }
}

/// <summary>
/// A study day is the learner's local date, which can be up to 14 hours ahead of UTC: anything later than tomorrow
/// (UTC) is a bad clock or a bad request.
/// </summary>
public sealed class ActivityDayPolicy(TimeProvider time)
{
    public const int MaxDaysPerWrite = 3_660;

    public Result Validate(IReadOnlyCollection<DateOnly> days)
    {
        ArgumentNullException.ThrowIfNull(days);
        var latest = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(1);
        if (days.Count > MaxDaysPerWrite)
        {
            return AppErrors.TooManyActivityDays;
        }

        return days.Any(day => day > latest) ? AppErrors.ActivityDayInFuture : Result.Success();
    }
}
