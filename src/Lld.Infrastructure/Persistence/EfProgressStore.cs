using Lld.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace Lld.Infrastructure.Persistence;

/// <summary>
/// <see cref="IProgressStore"/> for the API. Unlike the repositories behind a use case's unit of work, each call saves
/// at once: a progress write is one self-contained change, and the device store behaves the same way.
/// Only signed-in users have progress on the server; the guest lives on devices.
/// </summary>
internal sealed class EfProgressStore(LldDbContext db) : IProgressStore
{
    public async Task<ProgressSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken)
    {
        var ownerId = UserIdOf(owner);
        var lessons = await db.LessonProgress.AsNoTracking()
            .Where(row => row.OwnerId == ownerId)
            .OrderBy(row => row.LessonId)
            .ToListAsync(cancellationToken);
        var days = await db.ActivityDays.AsNoTracking()
            .Where(row => row.OwnerId == ownerId)
            .OrderBy(row => row.Day)
            .Select(row => row.Day)
            .ToListAsync(cancellationToken);
        return new ProgressSnapshot([.. lessons.Select(row => row.ToLesson())], days);
    }

    public async Task SaveLessonsAsync(ProgressOwner owner, IReadOnlyCollection<LessonProgress> lessons, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lessons);
        var ownerId = UserIdOf(owner);
        var stored = await TrackedRowsAsync(ownerId, lessons.Select(lesson => lesson.LessonId), cancellationToken);
        foreach (var lesson in lessons)
        {
            if (stored.TryGetValue(lesson.LessonId, out var row))
            {
                row.CopyFrom(ProgressRules.Newer(row.ToLesson(), lesson));
            }
            else
            {
                stored[lesson.LessonId] = db.LessonProgress.Add(LessonProgressRow.From(ownerId, lesson)).Entity;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddActivityDayAsync(ProgressOwner owner, DateOnly day, CancellationToken cancellationToken)
    {
        await AddMissingDaysAsync(UserIdOf(owner), [day], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProgressSnapshot> MergeAsync(ProgressOwner owner, ProgressSnapshot incoming, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        var ownerId = UserIdOf(owner);
        var merged = ProgressRules.Merge(await GetAsync(owner, cancellationToken), incoming);
        var stored = await TrackedRowsAsync(ownerId, merged.Lessons.Select(lesson => lesson.LessonId), cancellationToken);
        foreach (var lesson in merged.Lessons)
        {
            if (stored.TryGetValue(lesson.LessonId, out var row))
            {
                row.CopyFrom(lesson);
            }
            else
            {
                db.LessonProgress.Add(LessonProgressRow.From(ownerId, lesson));
            }
        }

        await AddMissingDaysAsync(ownerId, merged.ActivityDays, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return merged;
    }

    private static string UserIdOf(ProgressOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return owner.UserId ?? throw new ArgumentException("The server keeps progress for signed-in users only.", nameof(owner));
    }

    private async Task<Dictionary<string, LessonProgressRow>> TrackedRowsAsync(
        string ownerId, IEnumerable<string> lessonIds, CancellationToken cancellationToken)
    {
        var ids = lessonIds.Distinct(StringComparer.Ordinal).ToList();
        return await db.LessonProgress
            .Where(row => row.OwnerId == ownerId && ids.Contains(row.LessonId))
            .ToDictionaryAsync(row => row.LessonId, StringComparer.Ordinal, cancellationToken);
    }

    private async Task AddMissingDaysAsync(string ownerId, IReadOnlyCollection<DateOnly> days, CancellationToken cancellationToken)
    {
        var known = await db.ActivityDays
            .Where(row => row.OwnerId == ownerId && days.Contains(row.Day))
            .Select(row => row.Day)
            .ToListAsync(cancellationToken);
        db.ActivityDays.AddRange(days.Except(known).Select(day => new ActivityDayRow { OwnerId = ownerId, Day = day }));
    }
}
