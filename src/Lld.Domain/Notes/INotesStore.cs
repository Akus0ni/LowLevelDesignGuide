using Lld.Domain.Common;
using Lld.Domain.Progress;

namespace Lld.Domain.Notes;

/// <summary>
/// Repository for learners' notes and bookmarks, one owner at a time (spec 007 §3.9). The owner is the same
/// <see cref="ProgressOwner"/> as for progress: a signed-in user, or the device guest. Like <see cref="IProgressStore"/>,
/// it sits next to the model so one contract test suite holds for the device, API and in-memory stores (Liskov).
/// </summary>
public interface INotesStore
{
    /// <summary>The owner's notes and bookmarks, deleted ones included; <see cref="NotesSnapshot.Empty"/> when there are none.</summary>
    Task<NotesSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts or updates notes; per note the later edit wins (<see cref="NoteRules.Newer(Note, Note)"/>). Nothing is
    /// saved when the owner would go over <see cref="NoteLimits"/> totals.
    /// </summary>
    Task<Result> SaveNotesAsync(ProgressOwner owner, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken);

    /// <summary>Inserts or updates bookmarks by key; the later edit wins. Nothing is saved over the bookmark total.</summary>
    Task<Result> SaveBookmarksAsync(ProgressOwner owner, IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken);

    /// <summary>Merges <paramref name="incoming"/> into the owner's records (<see cref="NoteRules.Merge"/>), within the totals.</summary>
    /// <returns>The owner's records after the merge.</returns>
    Task<Result<NotesSnapshot>> MergeAsync(ProgressOwner owner, NotesSnapshot incoming, CancellationToken cancellationToken);
}
