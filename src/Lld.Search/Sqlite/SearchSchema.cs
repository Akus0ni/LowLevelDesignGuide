namespace Lld.Search.Sqlite;

/// <summary>
/// The search tables (plan §2.3), shared by the device's schema step V7 and the API's in-memory dev index.
/// <list type="bullet">
/// <item><c>SearchUnits</c>: one row per result (a lesson section, a cheat-sheet section, a note), with what a hit shows.</item>
/// <item><c>SearchStem</c> / <c>SearchRaw</c>: two external-content FTS5 indexes over its <c>Title</c>, <c>Heading</c> and
/// <c>Text</c>, stemmed and unstemmed (plan §2.2), kept in step by triggers.</item>
/// <item><c>SearchSynonyms</c> / <c>SearchSuggestions</c>: the vocabulary from <c>content/search.yml</c>.</item>
/// <item>Triggers on <c>Notes</c>: a note's unit changes in the same transaction as the note, so they never drift.</item>
/// </list>
/// Content units have <c>Owner = ''</c>; note units carry the owner's key.
/// </summary>
public static class SearchSchema
{
    public const string WholeLesson = "Whole lesson";

    /// <summary>Tables, indexes and the triggers that feed both FTS5 indexes.</summary>
    public const string TablesSql = """
        CREATE TABLE SearchUnits (
            Id           INTEGER PRIMARY KEY,
            Kind         TEXT    NOT NULL,
            Owner        TEXT    NOT NULL DEFAULT '',
            ModuleSlug   TEXT    NOT NULL DEFAULT '',
            ModuleOrder  INTEGER NOT NULL DEFAULT 0,
            LessonId     TEXT    NOT NULL,
            LessonSlug   TEXT    NOT NULL DEFAULT '',
            LessonTitle  TEXT    NOT NULL DEFAULT '',
            LessonOrder  INTEGER NOT NULL DEFAULT 0,
            SectionOrder INTEGER NOT NULL DEFAULT 0,
            Anchor       TEXT    NOT NULL DEFAULT '',
            Section      TEXT    NOT NULL DEFAULT '',
            NoteId       TEXT    NULL,
            Title        TEXT    NOT NULL DEFAULT '',
            Heading      TEXT    NOT NULL DEFAULT '',
            Text         TEXT    NOT NULL DEFAULT ''
        );
        CREATE INDEX IX_SearchUnits_Lesson ON SearchUnits (LessonId, Kind, Anchor);
        CREATE INDEX IX_SearchUnits_Note ON SearchUnits (Owner, NoteId) WHERE NoteId IS NOT NULL;
        CREATE VIRTUAL TABLE SearchStem USING fts5(
            Title, Heading, Text, content = 'SearchUnits', content_rowid = 'Id', tokenize = 'porter unicode61 remove_diacritics 2');
        CREATE VIRTUAL TABLE SearchRaw USING fts5(
            Title, Heading, Text, content = 'SearchUnits', content_rowid = 'Id', tokenize = 'unicode61 remove_diacritics 2');
        CREATE TRIGGER SearchUnits_Insert AFTER INSERT ON SearchUnits BEGIN
            INSERT INTO SearchStem (rowid, Title, Heading, Text) VALUES (new.Id, new.Title, new.Heading, new.Text);
            INSERT INTO SearchRaw (rowid, Title, Heading, Text) VALUES (new.Id, new.Title, new.Heading, new.Text);
        END;
        CREATE TRIGGER SearchUnits_Delete AFTER DELETE ON SearchUnits BEGIN
            INSERT INTO SearchStem (SearchStem, rowid, Title, Heading, Text) VALUES ('delete', old.Id, old.Title, old.Heading, old.Text);
            INSERT INTO SearchRaw (SearchRaw, rowid, Title, Heading, Text) VALUES ('delete', old.Id, old.Title, old.Heading, old.Text);
        END;
        CREATE TRIGGER SearchUnits_Update AFTER UPDATE ON SearchUnits BEGIN
            INSERT INTO SearchStem (SearchStem, rowid, Title, Heading, Text) VALUES ('delete', old.Id, old.Title, old.Heading, old.Text);
            INSERT INTO SearchRaw (SearchRaw, rowid, Title, Heading, Text) VALUES ('delete', old.Id, old.Title, old.Heading, old.Text);
            INSERT INTO SearchStem (rowid, Title, Heading, Text) VALUES (new.Id, new.Title, new.Heading, new.Text);
            INSERT INTO SearchRaw (rowid, Title, Heading, Text) VALUES (new.Id, new.Title, new.Heading, new.Text);
        END;
        CREATE TABLE SearchSynonyms (
            Word             TEXT NOT NULL PRIMARY KEY,
            AlternativesJson TEXT NOT NULL
        ) WITHOUT ROWID;
        CREATE TABLE SearchSuggestions (
            Position INTEGER PRIMARY KEY,
            Text     TEXT NOT NULL
        );
        """;

    /// <summary>
    /// Keeps a note's unit in step with the <c>Notes</c> table (spec 007's shape; only Owner, Id, LessonId, Anchor, Body
    /// and DeletedAt are read). The lesson and section names come from the lesson's content units, so a note on a removed
    /// section is indexed under "Whole lesson" and a note on a removed lesson has no module (spec §3.1).
    /// The insert trigger first removes the note's old unit: <c>INSERT OR REPLACE</c> (the device's upsert) deletes the old
    /// row without firing delete triggers, unless recursive triggers are on.
    /// </summary>
    public static string NoteTriggersSql { get; } = $"""
        CREATE TRIGGER Notes_Search_Insert AFTER INSERT ON Notes BEGIN
            DELETE FROM SearchUnits WHERE Owner = new.Owner AND NoteId = new.Id;
            {NoteUnitInsert("new", OneRow)} AND new.DeletedAt IS NULL;
        END;
        CREATE TRIGGER Notes_Search_Update AFTER UPDATE ON Notes BEGIN
            DELETE FROM SearchUnits WHERE Owner = old.Owner AND NoteId = old.Id;
            {NoteUnitInsert("new", OneRow)} AND new.DeletedAt IS NULL;
        END;
        CREATE TRIGGER Notes_Search_Delete AFTER DELETE ON Notes BEGIN
            DELETE FROM SearchUnits WHERE Owner = old.Owner AND NoteId = old.Id;
        END;
        """;

    /// <summary>Every live note's unit again, after the content changed (new lesson or section names).</summary>
    public static string ReindexNotesSql { get; } = $"""
        DELETE FROM SearchUnits WHERE Kind = 'note';
        {NoteUnitInsert("n", "Notes AS n")} AND n.DeletedAt IS NULL;
        """;

    /// <summary>The minimal <c>Notes</c> table for the API's in-memory index; the device has the full one (V6).</summary>
    public const string DevNotesTableSql = """
        CREATE TABLE Notes (
            Owner     TEXT NOT NULL,
            Id        TEXT NOT NULL,
            LessonId  TEXT NOT NULL,
            Anchor    TEXT NOT NULL DEFAULT '',
            Body      TEXT NOT NULL,
            DeletedAt TEXT NULL,
            PRIMARY KEY (Owner, Id)
        ) WITHOUT ROWID;
        """;

    /// <summary>A trigger reads its note from <c>new</c>, so the SELECT needs a one-row source to join from.</summary>
    private const string OneRow = "(SELECT 1) AS one";

    /// <summary>
    /// INSERT … SELECT of note units from <paramref name="source"/>, whose row is <paramref name="note"/> (<c>new</c> in a
    /// trigger, <c>n</c> when re-indexing). Ends in a WHERE clause, so callers can add <c>AND …</c>.
    /// </summary>
    private static string NoteUnitInsert(string note, string source) => $"""
        INSERT INTO SearchUnits (Kind, Owner, ModuleSlug, ModuleOrder, LessonId, LessonSlug, LessonTitle, LessonOrder,
                                 SectionOrder, Anchor, Section, NoteId, Title, Heading, Text)
            SELECT 'note', {note}.Owner, coalesce(l.ModuleSlug, ''), coalesce(l.ModuleOrder, 0), {note}.LessonId,
                   coalesce(l.LessonSlug, ''), coalesce(l.LessonTitle, ''), coalesce(l.LessonOrder, 0),
                   coalesce(s.SectionOrder, -1), {note}.Anchor, coalesce(s.Section, '{WholeLesson}'), {note}.Id,
                   coalesce(l.LessonTitle, ''), coalesce(s.Section, '{WholeLesson}'), {note}.Body
            FROM {source}
            LEFT JOIN SearchUnits AS l ON l.Owner = '' AND l.Kind = 'lesson' AND l.LessonId = {note}.LessonId AND l.Anchor = ''
            LEFT JOIN SearchUnits AS s ON {note}.Anchor <> '' AND s.Owner = '' AND s.Kind = 'lesson'
                                      AND s.LessonId = {note}.LessonId AND s.Anchor = {note}.Anchor
            WHERE 1 = 1
        """;
}
