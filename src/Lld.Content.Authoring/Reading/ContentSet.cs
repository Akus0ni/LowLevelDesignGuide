using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Reading;

/// <summary>Everything found under <c>content/</c>, parsed but not yet validated.</summary>
/// <param name="Issues">Problems found while reading (unreadable YAML); validation rules add the rest.</param>
public sealed record ContentSet(IReadOnlyList<SourceModule> Modules, IReadOnlyList<ContentIssue> Issues);

/// <param name="Slug">Folder name, e.g. <c>case-studies</c>.</param>
/// <param name="Path">Repository-relative folder path, e.g. <c>content/case-studies</c>.</param>
/// <param name="Manifest"><c>module.yml</c>, or <c>null</c> when it is missing or unreadable.</param>
public sealed record SourceModule(string Slug, string Path, YamlMap? Manifest, IReadOnlyList<SourceLesson> Lessons)
{
    public string ManifestPath => $"{Path}/{ContentReader.ModuleFileName}";
}

/// <param name="Lesson"><c>lesson.md</c>, or <c>null</c> when the file is missing.</param>
/// <param name="CheatSheet"><c>cheatsheet.md</c>, or <c>null</c> when the file is missing.</param>
public sealed record SourceLesson(string ModuleSlug, string Slug, string Path, SourceDocument? Lesson, SourceDocument? CheatSheet)
{
    public string LessonPath => $"{Path}/{ContentReader.LessonFileName}";

    public string CheatSheetPath => $"{Path}/{ContentReader.CheatSheetFileName}";
}
