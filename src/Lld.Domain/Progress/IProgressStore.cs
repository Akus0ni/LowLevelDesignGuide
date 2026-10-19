namespace Lld.Domain.Progress;

/// <summary>
/// Repository for learners' progress, one owner at a time. The device keeps it in SQLite, the API in EF Core.
/// It sits next to the model rather than in the application layer because the device has no application layer,
/// and one contract test suite must hold for every implementation (Liskov).
/// </summary>
public interface IProgressStore
{
    /// <summary>The owner's progress; <see cref="ProgressSnapshot.Empty"/> when there is none.</summary>
    Task<ProgressSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken);

    /// <summary>Inserts or updates records; per lesson the later edit wins (<see cref="ProgressRules.Newer"/>).</summary>
    Task SaveLessonsAsync(ProgressOwner owner, IReadOnlyCollection<LessonProgress> lessons, CancellationToken cancellationToken);

    /// <summary>Remembers that the owner studied on <paramref name="day"/> (local date). Saving a day twice is harmless.</summary>
    Task AddActivityDayAsync(ProgressOwner owner, DateOnly day, CancellationToken cancellationToken);

    /// <summary>Merges <paramref name="incoming"/> into the owner's progress (<see cref="ProgressRules.Merge(ProgressSnapshot, ProgressSnapshot)"/>).</summary>
    /// <returns>The owner's progress after the merge.</returns>
    Task<ProgressSnapshot> MergeAsync(ProgressOwner owner, ProgressSnapshot incoming, CancellationToken cancellationToken);
}
