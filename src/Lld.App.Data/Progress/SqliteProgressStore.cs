using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;

namespace Lld.App.Data.Progress;

/// <summary>
/// <see cref="IProgressStore"/> over the device database (plain SQLite, spec 003 R67). Every write is one transaction;
/// the merge and last-write-wins rules come from <see cref="ProgressRules"/>, so the device and the API agree.
/// </summary>
public sealed class SqliteProgressStore(DeviceDatabase database) : IProgressStore
{
    public async Task<ProgressSnapshot> GetAsync(ProgressOwner owner, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ReadAsync(connection, null, owner, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveLessonsAsync(ProgressOwner owner, IReadOnlyCollection<LessonProgress> lessons, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(lessons);
        await InTransactionAsync(async (connection, transaction) =>
        {
            var stored = (await ProgressRows.ReadLessonsAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false))
                .ToDictionary(lesson => lesson.LessonId, StringComparer.Ordinal);
            var winners = lessons
                .Select(lesson => stored.TryGetValue(lesson.LessonId, out var existing) ? ProgressRules.Newer(existing, lesson) : lesson)
                .ToList();
            await ProgressRows.UpsertLessonsAsync(connection, transaction, owner, winners, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddActivityDayAsync(ProgressOwner owner, DateOnly day, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        await InTransactionAsync(
            (connection, transaction) => ProgressRows.AddDaysAsync(connection, transaction, owner, [day], cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProgressSnapshot> MergeAsync(ProgressOwner owner, ProgressSnapshot incoming, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(incoming);
        ProgressSnapshot merged = ProgressSnapshot.Empty;
        await InTransactionAsync(async (connection, transaction) =>
            merged = await MergeInAsync(connection, transaction, owner, incoming, cancellationToken).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
        return merged;
    }

    /// <summary>
    /// Gives everything the device's guest read to <paramref name="user"/> and empties the guest (spec 005 §3.9), in one
    /// transaction: a crash never leaves the progress in both places or in neither.
    /// </summary>
    /// <returns><c>true</c> when there was guest progress to adopt.</returns>
    public async Task<bool> AdoptGuestAsync(ProgressOwner user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.IsGuest)
        {
            throw new ArgumentException("Guest progress can only be adopted by a signed-in user.", nameof(user));
        }

        var adopted = false;
        await InTransactionAsync(async (connection, transaction) =>
        {
            var guest = await ReadAsync(connection, transaction, ProgressOwner.Guest, cancellationToken).ConfigureAwait(false);
            if (guest.IsEmpty)
            {
                return;
            }

            await MergeInAsync(connection, transaction, user, guest, cancellationToken).ConfigureAwait(false);
            await ProgressRows.DeleteAllAsync(connection, transaction, ProgressOwner.Guest, cancellationToken).ConfigureAwait(false);
            adopted = true;
        }, cancellationToken).ConfigureAwait(false);
        return adopted;
    }

    private static async Task<ProgressSnapshot> MergeInAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, ProgressSnapshot incoming, CancellationToken cancellationToken)
    {
        var stored = await ReadAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false);
        var merged = ProgressRules.Merge(stored, incoming);
        await ProgressRows.UpsertLessonsAsync(connection, transaction, owner, merged.Lessons, cancellationToken).ConfigureAwait(false);
        await ProgressRows.AddDaysAsync(connection, transaction, owner, merged.ActivityDays, cancellationToken).ConfigureAwait(false);
        return merged;
    }

    private static async Task<ProgressSnapshot> ReadAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken) =>
        new(
            await ProgressRows.ReadLessonsAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false),
            await ProgressRows.ReadDaysAsync(connection, transaction, owner, cancellationToken).ConfigureAwait(false));

    private async Task InTransactionAsync(Func<SqliteConnection, SqliteTransaction, Task> work, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await work(connection, transaction).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
