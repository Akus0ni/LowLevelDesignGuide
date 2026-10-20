using Lld.Domain.Common;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data.Notes;

/// <summary>
/// <see cref="INotesStore"/> over the device database (plain SQLite, R67). Every write is one transaction that is rolled
/// back when the owner would go over a <see cref="NoteLimits"/> total; the rules come from <see cref="NoteRules"/>, so
/// the device and the API agree.
/// </summary>
public sealed class SqliteNotesStore(DeviceDatabase database) : INotesStore
{
    public async Task<NotesSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ReadAsync(connection, null, owner, cancellationToken).ConfigureAwait(false);
    }

    public Task<Result> SaveNotesAsync(ProgressOwner owner, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(notes);
        return InTransactionAsync(async (connection, transaction) =>
        {
            var winners = new List<Note>();
            foreach (var note in notes)
            {
                var stored = await NotesRows.FindNoteAsync(connection, transaction, owner, note.Id, cancellationToken).ConfigureAwait(false);
                winners.Add(stored is null ? note : NoteRules.Newer(stored, note));
            }

            await NotesRows.UpsertNotesAsync(connection, transaction, owner, winners, cancellationToken).ConfigureAwait(false);
            return await CheckNotesAsync(connection, transaction, owner, winners.Select(note => note.LessonId), cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<Result> SaveBookmarksAsync(ProgressOwner owner, IReadOnlyCollection<Bookmark> bookmarks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(bookmarks);
        return InTransactionAsync(async (connection, transaction) =>
        {
            var winners = new List<Bookmark>();
            foreach (var bookmark in bookmarks)
            {
                var stored = await NotesRows.FindBookmarkAsync(connection, transaction, owner, bookmark.Key, cancellationToken).ConfigureAwait(false);
                winners.Add(stored is null ? bookmark : NoteRules.Newer(stored, bookmark));
            }

            await NotesRows.UpsertBookmarksAsync(connection, transaction, owner, winners, cancellationToken).ConfigureAwait(false);
            return NoteLimits.CheckBookmarkTotal(
                await NotesRows.CountLiveBookmarksAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false));
        }, cancellationToken);
    }

    public async Task<Result<NotesSnapshot>> MergeAsync(ProgressOwner owner, NotesSnapshot incoming, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(incoming);
        var merged = NotesSnapshot.Empty;
        var result = await InTransactionAsync(async (connection, transaction) =>
        {
            var outcome = await MergeInAsync(connection, transaction, owner, incoming, cancellationToken).ConfigureAwait(false);
            if (outcome.IsSuccess)
            {
                merged = outcome.Value;
            }

            return outcome;
        }, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? merged : result.Error!;
    }

    /// <summary>
    /// Gives the device guest's notes and bookmarks to <paramref name="user"/> and empties the guest (spec 007 §3.8), in
    /// one transaction. Over a limit nothing moves: the guest keeps them for a later sign-in.
    /// </summary>
    /// <returns><c>true</c> when there was something to adopt and it was adopted.</returns>
    public async Task<Result<bool>> AdoptGuestAsync(ProgressOwner user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.IsGuest)
        {
            throw new ArgumentException("Guest notes can only be adopted by a signed-in user.", nameof(user));
        }

        var adopted = false;
        var result = await InTransactionAsync(async (connection, transaction) =>
        {
            var guest = await ReadAsync(connection, transaction, ProgressOwner.Guest, cancellationToken).ConfigureAwait(false);
            if (guest.IsEmpty)
            {
                return Result.Success();
            }

            var merged = await MergeInAsync(connection, transaction, user, guest, cancellationToken).ConfigureAwait(false);
            if (merged.IsFailure)
            {
                return merged;
            }

            await NotesRows.DeleteAllAsync(connection, transaction, ProgressOwner.Guest, cancellationToken).ConfigureAwait(false);
            adopted = true;
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? adopted : result.Error!;
    }

    private static async Task<Result<NotesSnapshot>> MergeInAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, NotesSnapshot incoming, CancellationToken cancellationToken)
    {
        var merged = NoteRules.Merge(await ReadAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false), incoming);
        await NotesRows.UpsertNotesAsync(connection, transaction, owner, merged.Notes, cancellationToken).ConfigureAwait(false);
        await NotesRows.UpsertBookmarksAsync(connection, transaction, owner, merged.Bookmarks, cancellationToken).ConfigureAwait(false);
        var totals = await CheckNotesAsync(connection, transaction, owner, merged.Notes.Select(note => note.LessonId), cancellationToken)
            .ConfigureAwait(false);
        if (totals.IsSuccess)
        {
            totals = NoteLimits.CheckBookmarkTotal(
                await NotesRows.CountLiveBookmarksAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false));
        }

        return totals.IsSuccess ? merged : totals.Error!;
    }

    private static async Task<Result> CheckNotesAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<string> lessonIds,
        CancellationToken cancellationToken)
    {
        var (total, byLesson) = await NotesRows.CountLiveNotesAsync(connection, transaction, owner, lessonIds, cancellationToken)
            .ConfigureAwait(false);
        return NoteLimits.CheckNoteTotals(total, byLesson);
    }

    private static async Task<NotesSnapshot> ReadAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken) =>
        NotesSnapshot.Of(
            await NotesRows.ReadNotesAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false),
            await NotesRows.ReadBookmarksAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false));

    /// <summary>Runs <paramref name="work"/> in one transaction, committed only when it succeeds.</summary>
    private async Task<Result> InTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task<Result>> work, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var result = await work(connection, transaction).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}
