using System.Globalization;
using Lld.Domain.Notes;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;
using static Lld.App.Data.DeviceSql;

namespace Lld.App.Data.Notes;

/// <summary>The SQL behind <see cref="SqliteNotesStore"/>: one method per statement, plus the row ↔ record mapping.</summary>
internal static class NotesRows
{
    private const string NoteColumns = "Id, LessonId, Anchor, Body, CreatedAt, UpdatedAt, DeletedAt, DeviceId";
    private const string BookmarkColumns = "Kind, LessonId, Anchor, CreatedAt, UpdatedAt, DeletedAt, DeviceId";

    public static async Task<IReadOnlyList<Note>> ReadNotesAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            $"SELECT {NoteColumns} FROM Notes WHERE Owner = $owner;", ("$owner", owner.Key));
        return await ReadAllAsync(command, ToNote, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Note?> FindNoteAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, Guid id, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            $"SELECT {NoteColumns} FROM Notes WHERE Owner = $owner AND Id = $id;", ("$owner", owner.Key), ("$id", IdText(id)));
        return (await ReadAllAsync(command, ToNote, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
    }

    public static async Task<IReadOnlyList<Bookmark>> ReadBookmarksAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            $"SELECT {BookmarkColumns} FROM Bookmarks WHERE Owner = $owner;", ("$owner", owner.Key));
        return await ReadAllAsync(command, ToBookmark, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<Bookmark?> FindBookmarkAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, BookmarkKey key, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            $"SELECT {BookmarkColumns} FROM Bookmarks WHERE Owner = $owner AND Kind = $kind AND LessonId = $lesson AND Anchor = $anchor;",
            ("$owner", owner.Key), ("$kind", (int)key.Kind), ("$lesson", key.LessonId), ("$anchor", key.Anchor));
        return (await ReadAllAsync(command, ToBookmark, cancellationToken).ConfigureAwait(false)).SingleOrDefault();
    }

    public static async Task UpsertNotesAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<Note> notes, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, $"""
            INSERT OR REPLACE INTO Notes (Owner, {NoteColumns})
            VALUES ($owner, $id, $lesson, $anchor, $body, $createdAt, $updatedAt, $deletedAt, $device);
            """);
        foreach (var note in notes)
        {
            Bind(command,
                ("$owner", owner.Key), ("$id", IdText(note.Id)), ("$lesson", note.LessonId), ("$anchor", note.Anchor), ("$body", note.Body),
                ("$createdAt", Text(note.CreatedAt)), ("$updatedAt", Text(note.UpdatedAt)), ("$deletedAt", Text(note.DeletedAt)),
                ("$device", note.DeviceId));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task UpsertBookmarksAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<Bookmark> bookmarks,
        CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, $"""
            INSERT OR REPLACE INTO Bookmarks (Owner, {BookmarkColumns})
            VALUES ($owner, $kind, $lesson, $anchor, $createdAt, $updatedAt, $deletedAt, $device);
            """);
        foreach (var bookmark in bookmarks)
        {
            Bind(command,
                ("$owner", owner.Key), ("$kind", (int)bookmark.Kind), ("$lesson", bookmark.LessonId), ("$anchor", bookmark.Anchor),
                ("$createdAt", Text(bookmark.CreatedAt)), ("$updatedAt", Text(bookmark.UpdatedAt)), ("$deletedAt", Text(bookmark.DeletedAt)),
                ("$device", bookmark.DeviceId));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>The owner's live (not deleted) notes: in all, and for each lesson in <paramref name="lessonIds"/>.</summary>
    public static async Task<(int Total, IReadOnlyDictionary<string, int> ByLesson)> CountLiveNotesAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<string> lessonIds,
        CancellationToken cancellationToken)
    {
        await using var total = Command(connection, transaction,
            "SELECT count(*) FROM Notes WHERE Owner = $owner AND DeletedAt IS NULL;", ("$owner", owner.Key));
        var count = Convert.ToInt32(await total.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);

        await using var perLesson = Command(connection, transaction,
            "SELECT count(*) FROM Notes WHERE Owner = $owner AND LessonId = $lesson AND DeletedAt IS NULL;");
        var byLesson = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var lessonId in lessonIds.Distinct(StringComparer.Ordinal))
        {
            Bind(perLesson, ("$owner", owner.Key), ("$lesson", lessonId));
            byLesson[lessonId] = Convert.ToInt32(
                await perLesson.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
        }

        return (count, byLesson);
    }

    public static async Task<int> CountLiveBookmarksAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            "SELECT count(*) FROM Bookmarks WHERE Owner = $owner AND DeletedAt IS NULL;", ("$owner", owner.Key));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture);
    }

    public static async Task DeleteAllAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            "DELETE FROM Notes WHERE Owner = $owner; DELETE FROM Bookmarks WHERE Owner = $owner;", ("$owner", owner.Key));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<T>> ReadAllAsync<T>(
        SqliteCommand command, Func<SqliteDataReader, T> map, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(map(reader));
        }

        return rows;
    }

    private static string IdText(Guid id) => id.ToString("D");

    private static Note ToNote(SqliteDataReader r) =>
        new(Guid.Parse(r.GetString(0)), r.GetString(1), r.GetString(2), r.GetString(3), Time(r, 4)!.Value, Time(r, 5)!.Value, Time(r, 6), r.GetString(7));

    private static Bookmark ToBookmark(SqliteDataReader r) =>
        new((BookmarkKind)r.GetInt32(0), r.GetString(1), r.GetString(2), Time(r, 3)!.Value, Time(r, 4)!.Value, Time(r, 5), r.GetString(6));
}
