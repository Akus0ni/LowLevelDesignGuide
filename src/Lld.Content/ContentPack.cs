using Lld.Contracts;

namespace Lld.Content;

/// <summary>
/// All lessons and cheat sheets, validated and packed at build time (<c>content-pack.json</c>).
/// The app loads it into its database; the dev API serves it from memory.
/// </summary>
/// <param name="Format">Shape of this file; readers reject formats they don't know.</param>
/// <param name="Version">Hash of the content: the same content always has the same version.</param>
public sealed record ContentPack(int Format, string Version, IReadOnlyList<PackedModule> Modules)
{
    public const int CurrentFormat = 1;
}

/// <param name="Slug">The module's folder name, e.g. <c>case-studies</c>.</param>
public sealed record PackedModule(string Slug, string Title, string Summary, int Order, IReadOnlyList<PackedLesson> Lessons);

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
    PackedCheatSheet CheatSheet);

public sealed record PackedCheatSheet(string Title, string BodyMarkdown);
