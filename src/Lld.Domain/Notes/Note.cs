namespace Lld.Domain.Notes;

/// <summary>
/// A learner's Markdown note on a lesson (spec 007 §3.1): on the whole lesson (<see cref="Anchor"/> = '') or on one
/// section (its heading id). A delete sets <see cref="DeletedAt"/> and keeps the record, so spec 010 can sync it;
/// an undo is a later write with no <see cref="DeletedAt"/>. Tags are never stored: <see cref="TagParser"/> reads them
/// from <see cref="Body"/>.
/// </summary>
public sealed record Note(
    Guid Id,
    string LessonId,
    string Anchor,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    string DeviceId)
{
    public bool IsDeleted => DeletedAt is not null;
}

/// <summary>What a bookmark points at.</summary>
public enum BookmarkKind
{
    Lesson = 0,
    Section = 1,
    CheatSheet = 2,
}

/// <summary>A bookmark's identity: at most one per owner and key. <see cref="Anchor"/> is '' except for sections.</summary>
public readonly record struct BookmarkKey(BookmarkKind Kind, string LessonId, string Anchor);

/// <summary>
/// A bookmarked lesson, section or cheat sheet (spec 007 §3.3). Removing it sets <see cref="DeletedAt"/>; bookmarking
/// it again is a later write with no <see cref="DeletedAt"/>.
/// </summary>
public sealed record Bookmark(
    BookmarkKind Kind,
    string LessonId,
    string Anchor,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt,
    string DeviceId)
{
    public BookmarkKey Key => new(Kind, LessonId, Anchor);

    public bool IsDeleted => DeletedAt is not null;
}

/// <summary>Everything one learner has written or bookmarked, deleted records included.</summary>
public sealed record NotesSnapshot(IReadOnlyList<Note> Notes, IReadOnlyList<Bookmark> Bookmarks)
{
    public static NotesSnapshot Empty { get; } = new([], []);

    /// <summary>
    /// A snapshot in the one order every store returns: oldest first, ties broken by id or key, so stores compare equal.
    /// </summary>
    public static NotesSnapshot Of(IEnumerable<Note> notes, IEnumerable<Bookmark> bookmarks) =>
        new(
            [.. notes.OrderBy(note => note.CreatedAt).ThenBy(note => note.Id)],
            [
                .. bookmarks
                    .OrderBy(bookmark => bookmark.CreatedAt)
                    .ThenBy(bookmark => bookmark.Kind)
                    .ThenBy(bookmark => bookmark.LessonId, StringComparer.Ordinal)
                    .ThenBy(bookmark => bookmark.Anchor, StringComparer.Ordinal),
            ]);

    public bool IsEmpty => Notes.Count == 0 && Bookmarks.Count == 0;
}
