namespace Lld.Contracts;

// Search on the wire (spec 009), shared by the API's dev endpoint and the MAUI bridge; the React app mirrors them in
// TypeScript (`web/src/data/types.ts`).

public static class SearchKinds
{
    public const string Lesson = "lesson";
    public const string CheatSheet = "cheatSheet";
    public const string Note = "note";

    public static IReadOnlyList<string> All { get; } = [Lesson, CheatSheet, Note];
}

/// <param name="Query">What the learner typed; parsed on the server side, never used as FTS5 syntax.</param>
/// <param name="Kind">One of <see cref="SearchKinds"/>, or null for all.</param>
/// <param name="Module">A module slug, or null for all modules.</param>
/// <param name="LessonId">Only this lesson: the reader asks for the words to mark (spec §3.6).</param>
public sealed record SearchRequestDto(string Query, string? Kind = null, string? Module = null, string? LessonId = null, int Limit = 50);

/// <param name="Marks">Matched ranges in <paramref name="Text"/>, each <c>[start, length]</c>.</param>
public sealed record SearchSnippetDto(string Text, IReadOnlyList<int[]> Marks);

/// <param name="Anchor">The section's heading id; empty for a lesson's intro or a whole-lesson note.</param>
/// <param name="Heading">The section's heading, or "Whole lesson".</param>
/// <param name="NoteId">The note, for note hits only.</param>
/// <param name="TitleMatch">A query word matched the lesson title (sorted first, spec Q1).</param>
public sealed record SearchHitDto(
    string Kind,
    string ModuleSlug,
    string LessonId,
    string LessonSlug,
    string LessonTitle,
    string Anchor,
    string Heading,
    string? NoteId,
    SearchSnippetDto Snippet,
    bool TitleMatch);

/// <param name="Expansions">Synonyms searched too ("single responsibility" for SRP).</param>
/// <param name="Total">Hits that pass every filter, before the limit.</param>
/// <param name="CountsByKind">Hits per kind for the filter chips: the module filter applies, the kind filter doesn't.</param>
/// <param name="Words">The matched words as written in the text, for marking them in the reader.</param>
/// <param name="Suggestions">Searches to offer, filled only when the query is too short to search (spec §3.4).</param>
public sealed record SearchResultsDto(
    string Query,
    IReadOnlyList<string> Expansions,
    int Total,
    IReadOnlyDictionary<string, int> CountsByKind,
    IReadOnlyList<SearchHitDto> Hits,
    IReadOnlyList<string> Words,
    IReadOnlyList<string> Suggestions);
