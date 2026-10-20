using Lld.Domain.Notes;

namespace Lld.Infrastructure.Persistence;

/// <summary>A <see cref="Note"/> as the API stores it: one row per user and note id. Mutable and flat because EF Core maps it.</summary>
public sealed class NoteRow
{
    public string OwnerId { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string LessonId { get; set; } = string.Empty;

    public string Anchor { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public static NoteRow From(string ownerId, Note note) => new NoteRow { OwnerId = ownerId, Id = note.Id }.CopyFrom(note);

    public NoteRow CopyFrom(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);
        LessonId = note.LessonId;
        Anchor = note.Anchor;
        Body = note.Body;
        CreatedAt = note.CreatedAt;
        UpdatedAt = note.UpdatedAt;
        DeletedAt = note.DeletedAt;
        DeviceId = note.DeviceId;
        return this;
    }

    public Note ToNote() => new(Id, LessonId, Anchor, Body, CreatedAt, UpdatedAt, DeletedAt, DeviceId);
}

/// <summary>A <see cref="Bookmark"/> as the API stores it: one row per user and bookmark key.</summary>
public sealed class BookmarkRow
{
    public string OwnerId { get; set; } = string.Empty;

    public BookmarkKind Kind { get; set; }

    public string LessonId { get; set; } = string.Empty;

    public string Anchor { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public BookmarkKey Key => new(Kind, LessonId, Anchor);

    public static BookmarkRow From(string ownerId, Bookmark bookmark) =>
        new BookmarkRow { OwnerId = ownerId, Kind = bookmark.Kind, LessonId = bookmark.LessonId, Anchor = bookmark.Anchor }.CopyFrom(bookmark);

    public BookmarkRow CopyFrom(Bookmark bookmark)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        CreatedAt = bookmark.CreatedAt;
        UpdatedAt = bookmark.UpdatedAt;
        DeletedAt = bookmark.DeletedAt;
        DeviceId = bookmark.DeviceId;
        return this;
    }

    public Bookmark ToBookmark() => new(Kind, LessonId, Anchor, CreatedAt, UpdatedAt, DeletedAt, DeviceId);
}
