using Lld.Domain.Common;

namespace Lld.Domain.Progress;

/// <summary>
/// What a progress write may contain. Records come from the web app (over the bridge or HTTP), so every store checks
/// them before saving: sizes stay bounded and a record's status always agrees with its completion fields.
/// </summary>
public static class ProgressLimits
{
    public const int MaxLessonIdLength = 120;
    public const int MaxSections = 200;
    public const int MaxAnchorLength = 200;
    public const int MaxDeviceIdLength = 64;
    public const int MaxLessonsPerWrite = 1_000;

    public static Result Validate(IReadOnlyCollection<LessonProgress> lessons)
    {
        ArgumentNullException.ThrowIfNull(lessons);
        if (lessons.Count > MaxLessonsPerWrite)
        {
            return Invalid("too_many", $"At most {MaxLessonsPerWrite} lessons can be saved at once.");
        }

        return lessons.Select(Validate).FirstOrDefault(result => result.IsFailure) ?? Result.Success();
    }

    public static Result Validate(LessonProgress lesson)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        return lesson switch
        {
            { LessonId: var id } when string.IsNullOrWhiteSpace(id) || id.Length > MaxLessonIdLength =>
                Invalid("lesson_id", $"A lesson id must have 1 to {MaxLessonIdLength} characters."),
            { ReadSections.Count: > MaxSections } =>
                Invalid("sections", $"A lesson can have at most {MaxSections} read sections."),
            _ when lesson.ReadSections.Any(anchor => anchor.Length is 0 or > MaxAnchorLength)
                   || lesson.Position?.Anchor.Length > MaxAnchorLength =>
                Invalid("anchor", $"A section anchor must have 1 to {MaxAnchorLength} characters."),
            { Position.Offset: < 0 } => Invalid("position", "A reading position can't be negative."),
            { DeviceId: var device } when device.Length > MaxDeviceIdLength =>
                Invalid("device_id", $"A device id has at most {MaxDeviceIdLength} characters."),
            _ when !Enum.IsDefined(lesson.Status) || (lesson.DoneBy is { } doneBy && !Enum.IsDefined(doneBy)) =>
                Invalid("status", $"Lesson '{lesson.LessonId}' has an unknown status."),
            _ when !HasConsistentCompletion(lesson) =>
                Invalid("status", $"Lesson '{lesson.LessonId}': only a done lesson has a completion (how and when)."),
            _ => Result.Success(),
        };
    }

    /// <summary>Done has both how and when it was completed; any other status has neither.</summary>
    private static bool HasConsistentCompletion(LessonProgress lesson) =>
        lesson.Status == LessonStatus.Done
            ? lesson.DoneBy is not null && lesson.CompletedAt is not null
            : lesson.DoneBy is null && lesson.CompletedAt is null;

    private static Error Invalid(string what, string message) => Error.Validation($"progress.{what}", message);
}
