namespace Lld.App.Data.Schema;

/// <summary>
/// Step 4 (spec 005 §3.10): learners' progress and study days, one row set per owner (a user or the device's guest).
/// No foreign key to <c>Lessons</c> on purpose: a content reload deletes and re-adds lessons, and progress for a lesson
/// that left the content must survive in case it comes back. Dates are ISO-8601 UTC text, which sorts correctly.
/// </summary>
public sealed class V4Progress : ISchemaMigration
{
    public int Version => 4;

    public string Name => "Progress";

    public string Sql => """
        CREATE TABLE LessonProgress (
            Owner            TEXT    NOT NULL,
            LessonId         TEXT    NOT NULL,
            Status           INTEGER NOT NULL,
            DoneBy           INTEGER NULL,
            CompletedAt      TEXT    NULL,
            ReadSectionsJson TEXT    NOT NULL DEFAULT '[]',
            PositionAnchor   TEXT    NULL,
            PositionOffset   INTEGER NULL,
            LastReadAt       TEXT    NULL,
            LastRevisedAt    TEXT    NULL,
            UpdatedAt        TEXT    NOT NULL,
            DeviceId         TEXT    NOT NULL,
            PRIMARY KEY (Owner, LessonId)
        ) WITHOUT ROWID;
        CREATE TABLE ActivityDays (
            Owner TEXT NOT NULL,
            Day   TEXT NOT NULL,
            PRIMARY KEY (Owner, Day)
        ) WITHOUT ROWID;
        """;
}
