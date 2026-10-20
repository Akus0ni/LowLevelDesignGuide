using Lld.Domain.Common;
using Lld.Domain.Notes;
using Lld.Domain.Progress;

namespace Lld.Progress.Tests.Notes;

/// <summary>The simplest store that keeps the contract: the reference the real stores are compared against.</summary>
public sealed class InMemoryNotesStore : INotesStore
{
    private readonly Dictionary<ProgressOwner, Dictionary<Guid, Note>> _notes = [];
    private readonly Dictionary<ProgressOwner, Dictionary<BookmarkKey, Bookmark>> _bookmarks = [];

    public Task<NotesSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken) =>
        Task.FromResult(NotesSnapshot.Of(NotesOf(owner).Values, BookmarksOf(owner).Values));

    public Task<Result> SaveNotesAsync(ProgressOwner owner, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        var after = new Dictionary<Guid, Note>(NotesOf(owner));
        foreach (var note in notes)
        {
            after[note.Id] = after.TryGetValue(note.Id, out var existing) ? NoteRules.Newer(existing, note) : note;
        }

        var totals = CheckNotes(after.Values);
        if (totals.IsSuccess)
        {
            _notes[owner] = after;
        }

        return Task.FromResult(totals);
    }

    public Task<Result> SaveBookmarksAsync(ProgressOwner owner, IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var after = new Dictionary<BookmarkKey, Bookmark>(BookmarksOf(owner));
        foreach (var bookmark in bookmarks)
        {
            after[bookmark.Key] = after.TryGetValue(bookmark.Key, out var existing) ? NoteRules.Newer(existing, bookmark) : bookmark;
        }

        var total = NoteLimits.CheckBookmarkTotal(after.Values.Count(bookmark => !bookmark.IsDeleted));
        if (total.IsSuccess)
        {
            _bookmarks[owner] = after;
        }

        return Task.FromResult(total);
    }

    public async Task<Result<NotesSnapshot>> MergeAsync(ProgressOwner owner, NotesSnapshot incoming, CancellationToken cancellationToken)
    {
        var merged = NoteRules.Merge(await GetAsync(owner, cancellationToken), incoming);
        var totals = CheckNotes(merged.Notes);
        if (totals.IsSuccess)
        {
            totals = NoteLimits.CheckBookmarkTotal(merged.Bookmarks.Count(bookmark => !bookmark.IsDeleted));
        }

        if (totals.IsFailure)
        {
            return totals.Error!;
        }

        _notes[owner] = merged.Notes.ToDictionary(note => note.Id);
        _bookmarks[owner] = merged.Bookmarks.ToDictionary(bookmark => bookmark.Key);
        return merged;
    }

    private static Result CheckNotes(IEnumerable<Note> notes)
    {
        var live = notes.Where(note => !note.IsDeleted).ToList();
        return NoteLimits.CheckNoteTotals(
            live.Count,
            live.GroupBy(note => note.LessonId, StringComparer.Ordinal).Select(lesson => KeyValuePair.Create(lesson.Key, lesson.Count())));
    }

    private Dictionary<Guid, Note> NotesOf(ProgressOwner owner) =>
        _notes.TryGetValue(owner, out var notes) ? notes : _notes[owner] = [];

    private Dictionary<BookmarkKey, Bookmark> BookmarksOf(ProgressOwner owner) =>
        _bookmarks.TryGetValue(owner, out var bookmarks) ? bookmarks : _bookmarks[owner] = [];
}
