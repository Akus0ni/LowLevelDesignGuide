using Lld.Domain.Common;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace Lld.Infrastructure.Persistence;

/// <summary>
/// <see cref="INotesStore"/> for the API. Each call saves at once, like <see cref="EfProgressStore"/>. The owner's rows are
/// loaded and changed in memory, the totals checked there, and nothing is saved when one is over its limit.
/// Only signed-in users have notes on the server; the guest lives on devices.
/// </summary>
internal sealed class EfNotesStore(LldDbContext db) : INotesStore
{
    public async Task<NotesSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken)
    {
        var ownerId = UserIdOf(owner);
        var notes = await db.Notes.AsNoTracking().Where(row => row.OwnerId == ownerId).ToListAsync(cancellationToken);
        var bookmarks = await db.Bookmarks.AsNoTracking().Where(row => row.OwnerId == ownerId).ToListAsync(cancellationToken);
        return NotesSnapshot.Of(notes.Select(row => row.ToNote()), bookmarks.Select(row => row.ToBookmark()));
    }

    public async Task<Result> SaveNotesAsync(ProgressOwner owner, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notes);
        var ownerId = UserIdOf(owner);
        var rows = await db.Notes.Where(row => row.OwnerId == ownerId).ToDictionaryAsync(row => row.Id, cancellationToken);
        foreach (var note in notes)
        {
            Upsert(ownerId, rows, rows.TryGetValue(note.Id, out var row) ? NoteRules.Newer(row.ToNote(), note) : note);
        }

        return await SaveWithinAsync(CheckNotes(rows.Values), cancellationToken);
    }

    public async Task<Result> SaveBookmarksAsync(ProgressOwner owner, IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bookmarks);
        var ownerId = UserIdOf(owner);
        var rows = await TrackedBookmarksAsync(ownerId, cancellationToken);
        foreach (var bookmark in bookmarks)
        {
            Upsert(ownerId, rows, rows.TryGetValue(bookmark.Key, out var row) ? NoteRules.Newer(row.ToBookmark(), bookmark) : bookmark);
        }

        return await SaveWithinAsync(CheckBookmarks(rows.Values), cancellationToken);
    }

    public async Task<Result<NotesSnapshot>> MergeAsync(ProgressOwner owner, NotesSnapshot incoming, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        var ownerId = UserIdOf(owner);
        var noteRows = await db.Notes.Where(row => row.OwnerId == ownerId).ToDictionaryAsync(row => row.Id, cancellationToken);
        var bookmarkRows = await TrackedBookmarksAsync(ownerId, cancellationToken);
        var stored = NotesSnapshot.Of(noteRows.Values.Select(row => row.ToNote()), bookmarkRows.Values.Select(row => row.ToBookmark()));
        var merged = NoteRules.Merge(stored, incoming);
        foreach (var note in merged.Notes)
        {
            Upsert(ownerId, noteRows, note);
        }

        foreach (var bookmark in merged.Bookmarks)
        {
            Upsert(ownerId, bookmarkRows, bookmark);
        }

        var totals = CheckNotes(noteRows.Values);
        var saved = await SaveWithinAsync(totals.IsSuccess ? CheckBookmarks(bookmarkRows.Values) : totals, cancellationToken);
        return saved.IsSuccess ? merged : saved.Error!;
    }

    private static string UserIdOf(ProgressOwner owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return owner.UserId ?? throw new ArgumentException("The server keeps notes for signed-in users only.", nameof(owner));
    }

    private static Result CheckNotes(IEnumerable<NoteRow> rows)
    {
        var live = rows.Where(row => row.DeletedAt is null).ToList();
        return NoteLimits.CheckNoteTotals(
            live.Count,
            live.GroupBy(row => row.LessonId, StringComparer.Ordinal).Select(lesson => KeyValuePair.Create(lesson.Key, lesson.Count())));
    }

    private static Result CheckBookmarks(IEnumerable<BookmarkRow> rows) =>
        NoteLimits.CheckBookmarkTotal(rows.Count(row => row.DeletedAt is null));

    private void Upsert(string ownerId, Dictionary<Guid, NoteRow> rows, Note note)
    {
        if (rows.TryGetValue(note.Id, out var row))
        {
            row.CopyFrom(note);
        }
        else
        {
            rows[note.Id] = db.Notes.Add(NoteRow.From(ownerId, note)).Entity;
        }
    }

    private void Upsert(string ownerId, Dictionary<BookmarkKey, BookmarkRow> rows, Bookmark bookmark)
    {
        if (rows.TryGetValue(bookmark.Key, out var row))
        {
            row.CopyFrom(bookmark);
        }
        else
        {
            rows[bookmark.Key] = db.Bookmarks.Add(BookmarkRow.From(ownerId, bookmark)).Entity;
        }
    }

    private async Task<Dictionary<BookmarkKey, BookmarkRow>> TrackedBookmarksAsync(string ownerId, CancellationToken cancellationToken) =>
        (await db.Bookmarks.Where(row => row.OwnerId == ownerId).ToListAsync(cancellationToken)).ToDictionary(row => row.Key);

    /// <summary>Saves the pending changes when <paramref name="totals"/> passed; otherwise forgets them.</summary>
    private async Task<Result> SaveWithinAsync(Result totals, CancellationToken cancellationToken)
    {
        if (totals.IsFailure)
        {
            db.ChangeTracker.Clear();
            return totals;
        }

        await db.SaveChangesAsync(cancellationToken);
        return totals;
    }
}
