using Lld.Domain.Common;

namespace Lld.Domain.Notes;

/// <summary>
/// What a notes write may contain (spec 007 §3.1). Records come from the web app (over the bridge or HTTP), so every
/// store checks them: each record on its own here, and the totals against what it already holds.
/// Deleted records keep their place for sync but never count towards a total.
/// </summary>
public static class NoteLimits
{
    public const int MaxBodyLength = 20_000;
    public const int MaxLessonIdLength = 120;
    public const int MaxAnchorLength = 200;
    public const int MaxDeviceIdLength = 64;
    public const int MaxRecordsPerWrite = 500;
    public const int MaxNotesPerLesson = 100;
    public const int MaxNotes = 5_000;
    public const int MaxBookmarks = 2_000;

    public static Result Validate(IReadOnlyCollection<Note> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        return notes.Count > MaxRecordsPerWrite
            ? TooManyInOneWrite()
            : notes.Select(Validate).FirstOrDefault(result => result.IsFailure) ?? Result.Success();
    }

    public static Result Validate(IReadOnlyCollection<Bookmark> bookmarks)
    {
        ArgumentNullException.ThrowIfNull(bookmarks);
        return bookmarks.Count > MaxRecordsPerWrite
            ? TooManyInOneWrite()
            : bookmarks.Select(Validate).FirstOrDefault(result => result.IsFailure) ?? Result.Success();
    }

    /// <summary>A live note has text; a deleted one may be empty (a note emptied and closed is deleted, spec §3.1).</summary>
    public static Result Validate(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);
        return note switch
        {
            { Id: var id } when id == Guid.Empty => Invalid("notes.id", "A note needs an id."),
            _ when InvalidLessonId(note.LessonId) => InvalidLessonIdError(),
            { Anchor.Length: > MaxAnchorLength } => InvalidAnchorError(),
            { Body.Length: > MaxBodyLength } => Invalid("notes.body", $"A note has at most {MaxBodyLength:N0} characters."),
            { IsDeleted: false } when string.IsNullOrWhiteSpace(note.Body) => Invalid("notes.body", "A note needs some text."),
            { DeviceId.Length: > MaxDeviceIdLength } => InvalidDeviceIdError(),
            { UpdatedAt: var updated, CreatedAt: var created } when updated < created =>
                Invalid("notes.dates", "A note can't be edited before it was written."),
            _ => Result.Success(),
        };
    }

    /// <summary>A section bookmark names its heading; a lesson or cheat-sheet bookmark doesn't.</summary>
    public static Result Validate(Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        return bookmark switch
        {
            { Kind: var kind } when !Enum.IsDefined(kind) => Invalid("bookmarks.kind", "A bookmark has an unknown kind."),
            _ when InvalidLessonId(bookmark.LessonId) => InvalidLessonIdError(),
            { Anchor.Length: > MaxAnchorLength } => InvalidAnchorError(),
            { Kind: BookmarkKind.Section, Anchor.Length: 0 } => Invalid("bookmarks.anchor", "A section bookmark needs the section."),
            { Kind: not BookmarkKind.Section, Anchor.Length: > 0 } =>
                Invalid("bookmarks.anchor", "Only a section bookmark names a section."),
            { DeviceId.Length: > MaxDeviceIdLength } => InvalidDeviceIdError(),
            _ => Result.Success(),
        };
    }

    /// <summary>Checks an owner's live notes after a write: in all, and for each lesson the write touched.</summary>
    public static Result CheckNoteTotals(int liveNotes, IEnumerable<KeyValuePair<string, int>> liveNotesByLesson)
    {
        ArgumentNullException.ThrowIfNull(liveNotesByLesson);
        if (liveNotes > MaxNotes)
        {
            return Invalid("notes.too_many", $"You can keep at most {MaxNotes:N0} notes.");
        }

        return liveNotesByLesson.FirstOrDefault(lesson => lesson.Value > MaxNotesPerLesson) is { Key: { } lessonId }
            ? Invalid("notes.too_many", $"A lesson can have at most {MaxNotesPerLesson} notes ('{lessonId}').")
            : Result.Success();
    }

    public static Result CheckBookmarkTotal(int liveBookmarks) =>
        liveBookmarks > MaxBookmarks
            ? Invalid("bookmarks.too_many", $"You can keep at most {MaxBookmarks:N0} bookmarks.")
            : Result.Success();

    private static bool InvalidLessonId(string lessonId) =>
        string.IsNullOrWhiteSpace(lessonId) || lessonId.Length > MaxLessonIdLength;

    private static Error InvalidLessonIdError() =>
        Error.Validation("notes.lesson_id", $"A lesson id must have 1 to {MaxLessonIdLength} characters.");

    private static Error InvalidAnchorError() =>
        Error.Validation("notes.anchor", $"A section anchor has at most {MaxAnchorLength} characters.");

    private static Error InvalidDeviceIdError() =>
        Error.Validation("notes.device_id", $"A device id has at most {MaxDeviceIdLength} characters.");

    private static Error TooManyInOneWrite() =>
        Error.Validation("notes.too_many", $"At most {MaxRecordsPerWrite} records can be saved at once.");

    private static Error Invalid(string code, string message) => Error.Validation(code, message);
}
