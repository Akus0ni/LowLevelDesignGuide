using Lld.App.Data.Progress;
using Lld.Contracts;
using Lld.Domain.Common;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Lld.Wire;

namespace Lld.App.Data.Notes;

/// <summary>
/// The device's notes and bookmarks as the bridge needs them (spec 007 §3.9): wire records in and out, the owner resolved
/// here (never sent by JavaScript), this device's id stamped on every write, and the limits checked before saving.
/// </summary>
public sealed class DeviceNotes(SqliteNotesStore store, DeviceIdentity device, IProgressOwnerSource owners)
{
    public async Task<NotesSnapshotDto> GetAsync(CancellationToken cancellationToken)
    {
        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        return NotesWire.ToDto(await store.GetAsync(owner, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result> SaveNotesAsync(IReadOnlyList<NoteDto> records, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);
        var parsed = NotesWire.ToNotes(records);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var deviceId = await device.GetIdAsync(cancellationToken).ConfigureAwait(false);
        var notes = parsed.Value.Select(note => note with { DeviceId = deviceId }).ToList();
        var valid = NoteLimits.Validate(notes);
        if (valid.IsFailure)
        {
            return valid;
        }

        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        return await store.SaveNotesAsync(owner, notes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> SaveBookmarksAsync(IReadOnlyList<BookmarkDto> records, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(records);
        var parsed = NotesWire.ToBookmarks(records);
        if (parsed.IsFailure)
        {
            return parsed.Error!;
        }

        var deviceId = await device.GetIdAsync(cancellationToken).ConfigureAwait(false);
        var bookmarks = parsed.Value.Select(bookmark => bookmark with { DeviceId = deviceId }).ToList();
        var valid = NoteLimits.Validate(bookmarks);
        if (valid.IsFailure)
        {
            return valid;
        }

        var owner = await owners.GetOwnerAsync(cancellationToken).ConfigureAwait(false);
        return await store.SaveBookmarksAsync(owner, bookmarks, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Hands the guest's notes and bookmarks to the user who just signed in (spec 007 §3.8).</summary>
    public async Task<Result> AdoptGuestAsync(string userId, CancellationToken cancellationToken)
    {
        var adopted = await store.AdoptGuestAsync(ProgressOwner.User(userId), cancellationToken).ConfigureAwait(false);
        return adopted.IsSuccess ? Result.Success() : adopted.Error!;
    }
}
