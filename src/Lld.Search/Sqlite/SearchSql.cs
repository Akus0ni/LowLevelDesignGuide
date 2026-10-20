namespace Lld.Search.Sqlite;

/// <summary>
/// The two queries behind a search (plan §2.2, §2.5). The text is assembled only from the fixed fragments below, by
/// which expressions are present; everything the learner typed travels as a parameter.
/// </summary>
internal static class SearchSql
{
    /// <summary>bm25 weights for Title, Heading, Text (spec Q1).</summary>
    private const string Weights = "10.0, 5.0, 1.0";

    /// <summary>About two lines of text around the first match (spec Q3).</summary>
    private const int SnippetTokens = 24;

    /// <summary>Phase 1: every matching unit with its rank and which columns matched; no snippets yet.</summary>
    public static string Candidates(bool hasPrefix, bool hasRest)
    {
        var raw = !hasPrefix
            ? string.Empty
            : $"""
                UNION ALL
                SELECT rowid, bm25(SearchRaw, {Weights}) FROM SearchRaw WHERE SearchRaw MATCH $prefix
                {(hasRest ? "AND rowid IN (SELECT rowid FROM SearchStem WHERE SearchStem MATCH $rest)" : string.Empty)}
              """;
        string RawToo(string parameter) => hasPrefix ? $"UNION SELECT rowid FROM SearchRaw WHERE SearchRaw MATCH {parameter}" : string.Empty;
        return $"""
            WITH hits(Id, Rank) AS MATERIALIZED (
                SELECT rowid, bm25(SearchStem, {Weights}) FROM SearchStem WHERE SearchStem MATCH $stem
                {raw}
            ),
            titled(Id) AS (
                SELECT rowid FROM SearchStem WHERE SearchStem MATCH $titleStem {RawToo("$titleRaw")}
            ),
            bodied(Id) AS (
                SELECT rowid FROM SearchStem WHERE SearchStem MATCH $bodyStem {RawToo("$bodyRaw")}
            )
            SELECT u.Id, u.Kind, u.ModuleSlug, u.ModuleOrder, u.LessonId, u.LessonSlug, u.LessonTitle, u.LessonOrder,
                   u.SectionOrder, u.Anchor, u.Section, u.NoteId, min(h.Rank) AS Rank,
                   u.Id IN (SELECT Id FROM titled) AS TitleMatch, u.Id IN (SELECT Id FROM bodied) AS BodyMatch
            FROM hits AS h JOIN SearchUnits AS u ON u.Id = h.Id
            WHERE u.Owner IN ('', $owner)
              AND ($module IS NULL OR u.ModuleSlug = $module)
              AND ($lesson IS NULL OR u.LessonId = $lesson)
            GROUP BY u.Id;
            """;
    }

    /// <summary>
    /// Phase 2, run once per index for the hits kept: the snippet of <c>Text</c>, the marked <c>Heading</c>, and with
    /// <paramref name="withText"/> the whole marked <c>Text</c> (the reader's words, spec §3.6). <c>$ids</c> is a JSON array.
    /// </summary>
    public static string Marks(string table, bool withText) => $"""
        SELECT rowid,
               snippet({table}, 2, $open, $close, '…', {SnippetTokens}),
               highlight({table}, 1, $open, $close),
               {(withText ? $"highlight({table}, 2, $open, $close)" : "NULL")}
        FROM {table}
        WHERE {table} MATCH $any AND rowid IN (SELECT value FROM json_each($ids));
        """;
}
