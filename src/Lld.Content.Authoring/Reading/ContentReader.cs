using Lld.Content.Authoring.Files;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Reading;

/// <summary>
/// Reads <c>content/&lt;module&gt;/module.yml</c> and <c>content/&lt;module&gt;/&lt;lesson&gt;/{lesson,cheatsheet}.md</c>.
/// Missing files are recorded as <c>null</c> for the validation rules to report; it never throws on bad content.
/// </summary>
public sealed class ContentReader(IContentFiles files, ContentOptions options)
{
    public const string ModuleFileName = "module.yml";
    public const string LessonFileName = "lesson.md";
    public const string CheatSheetFileName = "cheatsheet.md";
    public const string ContentMissingCode = "content.missing";

    private const int FirstLine = 1;

    public ContentSet Read()
    {
        var root = options.ContentDirectory;
        var issues = new List<ContentIssue>();
        if (!files.DirectoryExists(root))
        {
            issues.Add(new ContentIssue(root, null, ContentMissingCode, "The content folder doesn't exist."));
            return new ContentSet([], issues);
        }

        var modules = files.GetDirectories(root)
            .Where(IsContentFolder)
            .Select(slug => ReadModule(root, slug, issues))
            .ToList();
        return new ContentSet(modules, issues);
    }

    private SourceModule ReadModule(string root, string slug, List<ContentIssue> issues)
    {
        var path = $"{root}/{slug}";
        var manifestPath = $"{path}/{ModuleFileName}";
        YamlMap? manifest = null;
        if (files.FileExists(manifestPath))
        {
            var (map, issue) = YamlMapParser.Parse(files.ReadAllText(manifestPath), manifestPath, FirstLine);
            manifest = map;
            AddIfAny(issues, issue);
        }

        var lessons = files.GetDirectories(path)
            .Where(IsContentFolder)
            .Select(lessonSlug => ReadLesson(slug, $"{path}/{lessonSlug}", lessonSlug, issues))
            .ToList();
        return new SourceModule(slug, path, manifest, lessons);
    }

    private SourceLesson ReadLesson(string moduleSlug, string path, string slug, List<ContentIssue> issues) =>
        new(moduleSlug, slug, path, ReadDocument($"{path}/{LessonFileName}", issues), ReadDocument($"{path}/{CheatSheetFileName}", issues));

    private SourceDocument? ReadDocument(string path, List<ContentIssue> issues)
    {
        if (!files.FileExists(path))
        {
            return null;
        }

        var (document, issue) = SourceDocument.Parse(path, files.ReadAllText(path));
        AddIfAny(issues, issue);
        return document;
    }

    /// <summary>Folders starting with <c>.</c> or <c>_</c> are ignored (editor files, drafts).</summary>
    private static bool IsContentFolder(string name) => !name.StartsWith('.') && !name.StartsWith('_');

    private static void AddIfAny(List<ContentIssue> issues, ContentIssue? issue)
    {
        if (issue is not null)
        {
            issues.Add(issue);
        }
    }
}
