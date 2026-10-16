namespace Lld.Contracts;

// Read-only content contracts (spec 003), returned by the dev API and the MAUI bridge alike.
// The React app mirrors them in web/src/data/types.ts.

public sealed record ModuleSummary(string Slug, string Title, string Summary, int Order, int LessonCount);

/// <param name="Map">How the lessons relate (spec 004 §3.6); null when the module has none.</param>
public sealed record ModuleDetail(string Slug, string Title, string Summary, IReadOnlyList<LessonSummary> Lessons, ModuleMap? Map = null);

public sealed record LessonSummary(
    string Id,
    string ModuleSlug,
    string Slug,
    string Title,
    string Summary,
    int Order,
    int ReadingMinutes,
    IReadOnlyList<string> Tags);

/// <summary>An h2 or h3 heading of a lesson; <see cref="Anchor"/> follows GitHub's heading-id rules.</summary>
public sealed record OutlineEntry(int Level, string Text, string Anchor);

/// <summary>A type a lesson's code mentions, from its entities table (code callouts, spec 003 §3.8).</summary>
/// <param name="Anchor">Heading the entities table sits under ("Read more").</param>
public sealed record LessonTerm(string Term, string Role, string Anchor);

/// <summary>A neighbouring lesson in the same module, for previous / next links.</summary>
public sealed record LessonLink(string Id, string ModuleSlug, string Slug, string Title);

public sealed record LessonDetail(
    string Id,
    string ModuleSlug,
    string ModuleTitle,
    string Slug,
    string Title,
    string Summary,
    int Order,
    int ReadingMinutes,
    IReadOnlyList<string> Tags,
    string BodyMarkdown,
    IReadOnlyList<OutlineEntry> Outline,
    string CheatSheetTitle,
    LessonLink? Previous,
    LessonLink? Next,
    IReadOnlyList<LessonTerm> Terms);

public sealed record CheatSheetSummary(
    string LessonId,
    string ModuleSlug,
    string ModuleTitle,
    string LessonSlug,
    string LessonTitle,
    string Title);

public sealed record CheatSheetDetail(
    string LessonId,
    string ModuleSlug,
    string ModuleTitle,
    string LessonSlug,
    string LessonTitle,
    string Title,
    string BodyMarkdown);

public static class ContentErrorCodes
{
    /// <summary>The module, lesson or cheat sheet does not exist.</summary>
    public const string NotFound = "content.not_found";

    /// <summary>Dev mode only: the files under <c>content/</c> failed validation.</summary>
    public const string Invalid = "content.invalid";
}

/// <summary>A module map (spec 004 §3.6): lessons in titled groups, and labelled links between them.</summary>
public sealed record ModuleMap(IReadOnlyList<MapGroup> Groups, IReadOnlyList<MapLink> Links);

/// <param name="Title">Empty for lessons the author left outside any group.</param>
/// <param name="Lessons">Lesson slugs, in the order written.</param>
public sealed record MapGroup(string Title, IReadOnlyList<string> Lessons);

/// <summary>"<see cref="From"/> <see cref="Label"/> <see cref="To"/>", e.g. "ocp needs lsp". Both are lesson slugs.</summary>
public sealed record MapLink(string From, string To, string? Label);
