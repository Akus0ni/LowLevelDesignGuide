using System.Globalization;
using Lld.App.Data.Content;
using Lld.Domain.Progress;
using Microsoft.Data.Sqlite;
using static Lld.App.Data.DeviceSql;

namespace Lld.App.Data.Progress;

/// <summary>The SQL behind <see cref="SqliteProgressStore"/>: one method per statement, plus the row ↔ record mapping.</summary>
internal static class ProgressRows
{
    private const string DayFormat = "yyyy-MM-dd";

    private const string LessonColumns =
        "LessonId, Status, DoneBy, CompletedAt, ReadSectionsJson, PositionAnchor, PositionOffset, LastReadAt, LastRevisedAt, UpdatedAt, DeviceId";

    public static async Task<IReadOnlyList<LessonProgress>> ReadLessonsAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            $"SELECT {LessonColumns} FROM LessonProgress WHERE Owner = $owner ORDER BY LessonId;", ("$owner", owner.Key));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var lessons = new List<LessonProgress>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            lessons.Add(ToLesson(reader));
        }

        return lessons;
    }

    public static async Task<IReadOnlyList<DateOnly>> ReadDaysAsync(
        SqliteConnection connection, SqliteTransaction? transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            "SELECT Day FROM ActivityDays WHERE Owner = $owner ORDER BY Day;", ("$owner", owner.Key));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var days = new List<DateOnly>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            days.Add(DateOnly.ParseExact(reader.GetString(0), DayFormat, CultureInfo.InvariantCulture));
        }

        return days;
    }

    public static async Task UpsertLessonsAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<LessonProgress> lessons,
        CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, $"""
            INSERT OR REPLACE INTO LessonProgress (Owner, {LessonColumns})
            VALUES ($owner, $lesson, $status, $doneBy, $completedAt, $sections, $anchor, $offset, $readAt, $revisedAt, $updatedAt, $device);
            """);
        foreach (var lesson in lessons)
        {
            Bind(command,
                ("$owner", owner.Key), ("$lesson", lesson.LessonId), ("$status", (int)lesson.Status), ("$doneBy", (int?)lesson.DoneBy),
                ("$completedAt", Text(lesson.CompletedAt)), ("$sections", DeviceJson.Anchors(lesson.ReadSections)),
                ("$anchor", lesson.Position?.Anchor), ("$offset", lesson.Position?.Offset), ("$readAt", Text(lesson.LastReadAt)),
                ("$revisedAt", Text(lesson.LastRevisedAt)), ("$updatedAt", Text(lesson.UpdatedAt)), ("$device", lesson.DeviceId));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task AddDaysAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, IEnumerable<DateOnly> days,
        CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction, "INSERT OR IGNORE INTO ActivityDays (Owner, Day) VALUES ($owner, $day);");
        foreach (var day in days)
        {
            Bind(command, ("$owner", owner.Key), ("$day", day.ToString(DayFormat, CultureInfo.InvariantCulture)));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task DeleteAllAsync(
        SqliteConnection connection, SqliteTransaction transaction, ProgressOwner owner, CancellationToken cancellationToken)
    {
        await using var command = Command(connection, transaction,
            "DELETE FROM LessonProgress WHERE Owner = $owner; DELETE FROM ActivityDays WHERE Owner = $owner;", ("$owner", owner.Key));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static LessonProgress ToLesson(SqliteDataReader r) =>
        new(
            r.GetString(0),
            (LessonStatus)r.GetInt32(1),
            r.IsDBNull(2) ? null : (DoneBy)r.GetInt32(2),
            Time(r, 3),
            DeviceJson.Anchors(r.GetString(4)),
            r.IsDBNull(5) ? null : new ReadingPosition(r.GetString(5), r.GetInt32(6)),
            Time(r, 7),
            Time(r, 8),
            Time(r, 9)!.Value,
            r.GetString(10));
}
