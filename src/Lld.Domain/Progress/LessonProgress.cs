namespace Lld.Domain.Progress;

/// <summary>Where a learner is with one lesson. A lesson with no record is <see cref="NotStarted"/>.</summary>
public enum LessonStatus
{
    NotStarted = 0,
    InProgress = 1,
    Done = 2,
}

/// <summary>How a lesson became done: every section was read, or the learner marked it.</summary>
public enum DoneBy
{
    Auto = 0,
    Manual = 1,
}

/// <summary>Reading position: the nearest heading above the top of the screen ('' = above the first) and the pixels past it.</summary>
public sealed record ReadingPosition(string Anchor, int Offset);

/// <summary>
/// One learner's progress with one lesson (spec 005 §3.1). The reader changes it (status rules live in the web app);
/// the domain stores, compares and merges it. <see cref="UpdatedAt"/> and <see cref="DeviceId"/> are kept for the
/// last-write-wins sync of spec 010.
/// </summary>
public sealed record LessonProgress(
    string LessonId,
    LessonStatus Status,
    DoneBy? DoneBy,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> ReadSections,
    ReadingPosition? Position,
    DateTimeOffset? LastReadAt,
    DateTimeOffset? LastRevisedAt,
    DateTimeOffset UpdatedAt,
    string DeviceId);

/// <summary>Everything one learner has: lesson records and the local days they studied on.</summary>
public sealed record ProgressSnapshot(IReadOnlyList<LessonProgress> Lessons, IReadOnlyList<DateOnly> ActivityDays)
{
    public static ProgressSnapshot Empty { get; } = new([], []);

    public bool IsEmpty => Lessons.Count == 0 && ActivityDays.Count == 0;
}
