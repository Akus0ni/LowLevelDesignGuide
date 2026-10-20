using Lld.Domain.Notes;

namespace Lld.Domain.Tests.Notes;

/// <summary>Readable test records: "written at T0" and change only what the test is about.</summary>
internal static class NoteBuilder
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    public static Note Written(string body = "Ask about #srp", string lessonId = "srp", string anchor = "", int minutes = 0, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), lessonId, anchor, body, At(0), At(minutes), null, "device-a");

    public static Bookmark Marked(BookmarkKind kind = BookmarkKind.Lesson, string lessonId = "srp", string anchor = "", int minutes = 0) =>
        new(kind, lessonId, anchor, At(0), At(minutes), null, "device-a");

    public static DateTimeOffset At(int minutes) => T0.AddMinutes(minutes);
}
