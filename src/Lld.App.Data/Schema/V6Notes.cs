namespace Lld.App.Data.Schema;

/// <summary>
/// Step 6 (spec 007 §3.9): learners' notes and bookmarks, one row set per owner (a user or the device's guest).
/// As with progress, no foreign key to <c>Lessons</c>: a content reload deletes and re-adds lessons, and notes on a
/// lesson that left the content are kept (spec §3.2). Deletes are soft (<c>DeletedAt</c>) so spec 010 can sync them.
/// </summary>
public sealed class V6Notes : ISchemaMigration
{
    public int Version => 6;

    public string Name => "Notes and bookmarks";

    public string Sql => """
        CREATE TABLE Notes (
            Owner     TEXT NOT NULL,
            Id        TEXT NOT NULL,
            LessonId  TEXT NOT NULL,
            Anchor    TEXT NOT NULL DEFAULT '',
            Body      TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            DeletedAt TEXT NULL,
            DeviceId  TEXT NOT NULL,
            PRIMARY KEY (Owner, Id)
        ) WITHOUT ROWID;
        CREATE INDEX IX_Notes_Owner_Lesson ON Notes (Owner, LessonId);
        CREATE TABLE Bookmarks (
            Owner     TEXT    NOT NULL,
            Kind      INTEGER NOT NULL,
            LessonId  TEXT    NOT NULL,
            Anchor    TEXT    NOT NULL DEFAULT '',
            CreatedAt TEXT    NOT NULL,
            UpdatedAt TEXT    NOT NULL,
            DeletedAt TEXT    NULL,
            DeviceId  TEXT    NOT NULL,
            PRIMARY KEY (Owner, Kind, LessonId, Anchor)
        ) WITHOUT ROWID;
        """;
}
