using Lld.Domain.Progress;

namespace Lld.Progress.Tests;

/// <summary>The simplest store that keeps the contract: the reference the real stores are compared against.</summary>
public sealed class InMemoryProgressStore : IProgressStore
{
    private readonly Dictionary<ProgressOwner, Dictionary<string, LessonProgress>> _lessons = [];
    private readonly Dictionary<ProgressOwner, SortedSet<DateOnly>> _days = [];

    public Task<ProgressSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken) =>
        Task.FromResult(new ProgressSnapshot(
            [.. LessonsOf(owner).Values.OrderBy(lesson => lesson.LessonId, StringComparer.Ordinal)],
            [.. DaysOf(owner)]));

    public Task SaveLessonsAsync(ProgressOwner owner, IReadOnlyCollection<LessonProgress> lessons, CancellationToken cancellationToken)
    {
        var stored = LessonsOf(owner);
        foreach (var lesson in lessons)
        {
            stored[lesson.LessonId] = stored.TryGetValue(lesson.LessonId, out var existing) ? ProgressRules.Newer(existing, lesson) : lesson;
        }

        return Task.CompletedTask;
    }

    public Task AddActivityDayAsync(ProgressOwner owner, DateOnly day, CancellationToken cancellationToken)
    {
        DaysOf(owner).Add(day);
        return Task.CompletedTask;
    }

    public async Task<ProgressSnapshot> MergeAsync(ProgressOwner owner, ProgressSnapshot incoming, CancellationToken cancellationToken)
    {
        var merged = ProgressRules.Merge(await GetAsync(owner, cancellationToken), incoming);
        _lessons[owner] = merged.Lessons.ToDictionary(lesson => lesson.LessonId, StringComparer.Ordinal);
        _days[owner] = [.. merged.ActivityDays];
        return merged;
    }

    private Dictionary<string, LessonProgress> LessonsOf(ProgressOwner owner) =>
        _lessons.TryGetValue(owner, out var lessons) ? lessons : _lessons[owner] = new(StringComparer.Ordinal);

    private SortedSet<DateOnly> DaysOf(ProgressOwner owner) =>
        _days.TryGetValue(owner, out var days) ? days : _days[owner] = [];
}
