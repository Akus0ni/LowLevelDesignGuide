using Lld.Domain.Progress;

namespace Lld.Domain.Tests.Progress;

/// <summary>Readable test records: start from "opened at T0" and change only what the test is about.</summary>
internal static class ProgressBuilder
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    public static LessonProgress Opened(string lessonId = "srp", int minutes = 0) =>
        new(lessonId, LessonStatus.InProgress, null, null, [], null, At(minutes), null, At(minutes), "device-a");

    public static LessonProgress Done(string lessonId = "srp", int minutes = 0, DoneBy by = DoneBy.Auto) =>
        Opened(lessonId, minutes) with { Status = LessonStatus.Done, DoneBy = by, CompletedAt = At(minutes) };

    public static DateTimeOffset At(int minutes) => T0.AddMinutes(minutes);
}
