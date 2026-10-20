using Lld.Contracts;

namespace Lld.Content;

/// <summary>
/// All lessons and cheat sheets, validated and packed at build time (<c>content-pack.json</c>).
/// The app loads it into its database; the dev API serves it from memory.
/// </summary>
/// <param name="Format">Shape of this file; readers reject formats they don't know.</param>
/// <param name="Version">Hash of the content: the same content always has the same version.</param>
/// <param name="Vocabulary">Synonyms and suggestions from <c>content/search.yml</c> (spec 009 §3.3).</param>
public sealed record ContentPack(int Format, string Version, IReadOnlyList<PackedModule> Modules, SearchVocabulary? Vocabulary = null)
{
    /// <summary>
    /// 4: lessons and cheat sheets carry their search text, the pack a search vocabulary (spec 009). 3: lessons carry a
    /// symbol, modules a picker and look-alikes (spec 006). 2: modules carry a map (spec 004).
    /// </summary>
    public const int CurrentFormat = 4;
}

/// <param name="Slug">The module's folder name, e.g. <c>case-studies</c>.</param>
/// <param name="Map">The module map (spec 004 §3.6), if the module has one.</param>
/// <param name="Picker">The "Which pattern?" tree (spec 006 §3.6), if the module has one.</param>
/// <param name="Lookalikes">Look-alike groups (spec 006 §3.7), if any.</param>
public sealed record PackedModule(
    string Slug,
    string Title,
    string Summary,
    int Order,
    IReadOnlyList<PackedLesson> Lessons,
    ModuleMap? Map = null,
    IReadOnlyList<Mnemonic>? Mnemonics = null,
    ModulePicker? Picker = null,
    IReadOnlyList<Lookalike>? Lookalikes = null);

/// <param name="Id">Stable id from the front-matter; progress, notes and bookmarks refer to it.</param>
/// <param name="Slug">The lesson's folder name, used in routes.</param>
/// <param name="BodyMarkdown">Markdown without front-matter, links already rewritten to app routes or GitHub.</param>
public sealed record PackedLesson(
    string Id,
    string Slug,
    string Title,
    string Summary,
    int Order,
    IReadOnlyList<string> Tags,
    string? Source,
    int ReadingMinutes,
    string BodyMarkdown,
    IReadOnlyList<OutlineEntry> Outline,
    PackedCheatSheet CheatSheet,
    IReadOnlyList<LessonTerm> Terms,
    string? Icon = null,
    string? Hook = null,
    string? Symbol = null,
    IReadOnlyList<SearchSection>? Search = null);

/// <param name="Search">The cheat sheet's search text, one entry per <c>##</c> section (spec 009 §3.1).</param>
public sealed record PackedCheatSheet(string Title, string BodyMarkdown, IReadOnlyList<SearchSection>? Search = null);

/// <summary>
/// The plain text of one <c>##</c> section, as search indexes it (spec 009 §3.1): Markdown stripped, code and diagrams
/// left out, folded content kept.
/// </summary>
/// <param name="Anchor">The heading's id, as in the outline; empty for the text before the first <c>##</c>.</param>
/// <param name="Heading">The heading as shown; empty for the intro.</param>
public sealed record SearchSection(string Anchor, string Heading, string Text);

/// <param name="Synonyms">Lower-case word → the alternatives it also finds (one-way).</param>
/// <param name="Suggestions">Searches offered while the box is empty.</param>
public sealed record SearchVocabulary(IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms, IReadOnlyList<string> Suggestions)
{
    public static SearchVocabulary Empty { get; } = new(new Dictionary<string, IReadOnlyList<string>>(), []);
}
