using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Domain.Notes;

namespace Lld.Wire;

/// <summary>
/// Notes and bookmarks ↔ their DTOs (spec 007). Reading from the wire can fail (a bad id, an unknown kind), so those
/// directions return a <see cref="Result"/> instead of throwing. Limits are checked by <see cref="NoteLimits"/> afterwards.
/// </summary>
public static class NotesWire
{
    private static readonly Error BadId = Error.Validation("notes.id", "A note id must be a GUID.");
    private static readonly Error UnknownKind = Error.Validation("bookmarks.kind", "Unknown bookmark kind.");

    public static NotesSnapshotDto ToDto(NotesSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new NotesSnapshotDto([.. snapshot.Notes.Select(ToDto)], [.. snapshot.Bookmarks.Select(ToDto)]);
    }

    public static NoteDto ToDto(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);
        return new NoteDto(note.Id.ToString("D"), note.LessonId, note.Anchor, note.Body, note.CreatedAt, note.UpdatedAt, note.DeletedAt, note.DeviceId);
    }

    public static BookmarkDto ToDto(Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        return new BookmarkDto(
            ToText(bookmark.Kind), bookmark.LessonId, bookmark.Anchor, bookmark.CreatedAt, bookmark.UpdatedAt, bookmark.DeletedAt, bookmark.DeviceId);
    }

    public static Result<NotesSnapshot> ToSnapshot(NotesSnapshotDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var notes = ToNotes(dto.Notes ?? []);
        if (notes.IsFailure)
        {
            return notes.Error!;
        }

        var bookmarks = ToBookmarks(dto.Bookmarks ?? []);
        return bookmarks.IsFailure ? bookmarks.Error! : new NotesSnapshot(notes.Value, bookmarks.Value);
    }

    public static Result<IReadOnlyList<Note>> ToNotes(IEnumerable<NoteDto> dtos) => All(dtos, ToNote);

    public static Result<IReadOnlyList<Bookmark>> ToBookmarks(IEnumerable<BookmarkDto> dtos) => All(dtos, ToBookmark);

    public static Result<Note> ToNote(NoteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return Guid.TryParse(dto.Id, out var id)
            ? new Note(
                id, dto.LessonId ?? string.Empty, dto.Anchor ?? string.Empty, dto.Body ?? string.Empty,
                dto.CreatedAt, dto.UpdatedAt, dto.DeletedAt, dto.DeviceId ?? string.Empty)
            : BadId;
    }

    public static Result<Bookmark> ToBookmark(BookmarkDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return TryParseKind(dto.Kind, out var kind)
            ? new Bookmark(
                kind, dto.LessonId ?? string.Empty, dto.Anchor ?? string.Empty, dto.CreatedAt, dto.UpdatedAt, dto.DeletedAt, dto.DeviceId ?? string.Empty)
            : UnknownKind;
    }

    private static Result<IReadOnlyList<T>> All<TDto, T>(IEnumerable<TDto> dtos, Func<TDto, Result<T>> convert)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        var records = new List<T>();
        foreach (var dto in dtos)
        {
            var record = convert(dto);
            if (record.IsFailure)
            {
                return record.Error!;
            }

            records.Add(record.Value);
        }

        return records;
    }

    private static bool TryParseKind(string? text, out BookmarkKind kind)
    {
        (var known, kind) = text switch
        {
            BookmarkKinds.Lesson => (true, BookmarkKind.Lesson),
            BookmarkKinds.Section => (true, BookmarkKind.Section),
            BookmarkKinds.CheatSheet => (true, BookmarkKind.CheatSheet),
            _ => (false, default),
        };
        return known;
    }

    private static string ToText(BookmarkKind kind) => kind switch
    {
        BookmarkKind.Lesson => BookmarkKinds.Lesson,
        BookmarkKind.Section => BookmarkKinds.Section,
        BookmarkKind.CheatSheet => BookmarkKinds.CheatSheet,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bookmark kind."),
    };
}
