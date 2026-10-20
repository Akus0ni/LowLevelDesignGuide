using Lld.Domain.Common;
using Lld.Domain.Notes;
using Lld.Domain.Progress;

namespace Lld.Application.Notes;

// Notes and bookmarks use cases for the API (spec 007 §3.9). Every one acts on the signed-in user only: the user id
// comes from the access token, never from the request. Records are checked one by one here and the totals in the store.

public sealed class GetNotes(INotesStore store)
{
    public Task<NotesSnapshot> ExecuteAsync(string userId, CancellationToken cancellationToken) =>
        store.GetAsync(ProgressOwner.User(userId), cancellationToken);
}

/// <summary>Saves notes (an edit, a delete or an undo); per note the later edit wins.</summary>
public sealed class SaveNotes(INotesStore store)
{
    public async Task<Result> ExecuteAsync(string userId, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        var valid = NoteLimits.Validate(notes);
        return valid.IsFailure ? valid : await store.SaveNotesAsync(ProgressOwner.User(userId), notes, cancellationToken);
    }
}

/// <summary>Saves bookmarks (added or removed); per bookmark the later edit wins.</summary>
public sealed class SaveBookmarks(INotesStore store)
{
    public async Task<Result> ExecuteAsync(string userId, IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        var valid = NoteLimits.Validate(bookmarks);
        return valid.IsFailure ? valid : await store.SaveBookmarksAsync(ProgressOwner.User(userId), bookmarks, cancellationToken);
    }
}

/// <summary>Merges what a browser guest wrote and bookmarked into the user who just signed in (spec 007 §3.8).</summary>
public sealed class AdoptGuestNotes(INotesStore store)
{
    public async Task<Result<NotesSnapshot>> ExecuteAsync(string userId, NotesSnapshot guest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(guest);
        var valid = NoteLimits.Validate(guest.Notes);
        if (valid.IsSuccess)
        {
            valid = NoteLimits.Validate(guest.Bookmarks);
        }

        return valid.IsFailure ? valid.Error! : await store.MergeAsync(ProgressOwner.User(userId), guest, cancellationToken);
    }
}
