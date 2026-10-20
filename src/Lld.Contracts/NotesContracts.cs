namespace Lld.Contracts;

// Notes and bookmarks on the wire (spec 007), shared by the API and the MAUI bridge; the React app mirrors them in
// TypeScript. Kinds are strings and note ids are GUID text, so every client reads them the same way.

public static class BookmarkKinds
{
    public const string Lesson = "lesson";
    public const string Section = "section";
    public const string CheatSheet = "cheatSheet";
}

/// <param name="Id">A GUID made by the client when the note is first written.</param>
/// <param name="Anchor">The section's heading id, or empty for the whole lesson.</param>
/// <param name="DeletedAt">Set when the note was deleted; the record stays so sync can carry the delete.</param>
public sealed record NoteDto(
    string Id,
    string LessonId,
    string Anchor,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    string DeviceId);

/// <param name="Kind">One of <see cref="BookmarkKinds"/>.</param>
/// <param name="Anchor">The section's heading id for a section bookmark, otherwise empty.</param>
public sealed record BookmarkDto(
    string Kind,
    string LessonId,
    string Anchor,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    string DeviceId);

/// <summary>Everything one learner has written or bookmarked, deleted records included, oldest first.</summary>
public sealed record NotesSnapshotDto(IReadOnlyList<NoteDto> Notes, IReadOnlyList<BookmarkDto> Bookmarks);
