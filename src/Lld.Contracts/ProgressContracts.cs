namespace Lld.Contracts;

// Progress on the wire (spec 005), shared by the API and the MAUI bridge; the React app mirrors it in TypeScript.
// Statuses and days are strings so every client reads them the same way, whatever its JSON settings.

public static class ProgressStatuses
{
    public const string NotStarted = "notStarted";
    public const string InProgress = "inProgress";
    public const string Done = "done";
}

public static class ProgressDoneBy
{
    public const string Auto = "auto";
    public const string Manual = "manual";
}

public sealed record ReadingPositionDto(string Anchor, int Offset);

/// <param name="Status">One of <see cref="ProgressStatuses"/>.</param>
/// <param name="DoneBy">One of <see cref="ProgressDoneBy"/>, only when done.</param>
public sealed record LessonProgressDto(
    string LessonId,
    string Status,
    string? DoneBy,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> ReadSections,
    ReadingPositionDto? Position,
    DateTimeOffset? LastReadAt,
    DateTimeOffset? LastRevisedAt,
    DateTimeOffset UpdatedAt,
    string DeviceId);

/// <param name="ActivityDays">Local days as <c>yyyy-MM-dd</c>, oldest first.</param>
public sealed record ProgressSnapshotDto(IReadOnlyList<LessonProgressDto> Lessons, IReadOnlyList<string> ActivityDays);

/// <param name="Day">The learner's local date, <c>yyyy-MM-dd</c>.</param>
public sealed record ActivityDayRequest(string Day);
