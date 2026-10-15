namespace Lld.App.Data.Schema;

/// <summary>Step 1 (spec 003): device facts and the bundled content. Content rows are replaced whenever the pack changes.</summary>
public sealed class V1ContentTables : ISchemaMigration
{
    public int Version => 1;

    public string Name => "Content tables";

    public string Sql => """
        CREATE TABLE DeviceMeta (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL
        );

        CREATE TABLE Modules (
            Slug      TEXT    NOT NULL PRIMARY KEY,
            Title     TEXT    NOT NULL,
            Summary   TEXT    NOT NULL,
            SortOrder INTEGER NOT NULL
        );

        CREATE TABLE Lessons (
            Id                 TEXT    NOT NULL PRIMARY KEY,
            ModuleSlug         TEXT    NOT NULL REFERENCES Modules (Slug) ON DELETE CASCADE,
            Slug               TEXT    NOT NULL,
            Title              TEXT    NOT NULL,
            Summary            TEXT    NOT NULL,
            SortOrder          INTEGER NOT NULL,
            TagsJson           TEXT    NOT NULL,
            Source             TEXT    NULL,
            ReadingMinutes     INTEGER NOT NULL,
            BodyMarkdown       TEXT    NOT NULL,
            OutlineJson        TEXT    NOT NULL,
            CheatSheetTitle    TEXT    NOT NULL,
            CheatSheetMarkdown TEXT    NOT NULL,
            UNIQUE (ModuleSlug, Slug)
        );

        CREATE INDEX IX_Lessons_Module_SortOrder ON Lessons (ModuleSlug, SortOrder);
        """;
}
