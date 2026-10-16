using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary><c>module.yml</c> follows <see cref="ContentSchemas.Module"/>.</summary>
public sealed class ModuleManifestSchemaRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules
            .Where(m => m.Manifest is not null)
            .SelectMany(m => ContentSchemas.Module.Check(m.Manifest!, m.ManifestPath));
}

/// <summary>Each <c>lesson.md</c> front-matter follows <see cref="ContentSchemas.Lesson"/>.</summary>
public sealed class LessonFrontMatterRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Lessons
            .Select(l => l.Lesson)
            .OfType<SourceDocument>()
            .SelectMany(d => FrontMatterCheck.Run(context, d, ContentSchemas.Lesson));
}

/// <summary>Each <c>cheatsheet.md</c> front-matter follows <see cref="ContentSchemas.CheatSheet"/>.</summary>
public sealed class CheatSheetFrontMatterRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Lessons
            .Select(l => l.CheatSheet)
            .OfType<SourceDocument>()
            .SelectMany(d => FrontMatterCheck.Run(context, d, ContentSchemas.CheatSheet));
}

internal static class FrontMatterCheck
{
    private const int FirstLine = 1;

    public static IEnumerable<ContentIssue> Run(ContentContext context, SourceDocument document, FrontMatterSchema schema)
    {
        if (document.FrontMatter is { } map)
        {
            return schema.Check(map, document.Path);
        }

        // Unreadable YAML was already reported by the reader.
        return context.WasUnreadable(document.Path)
            ? []
            : [new ContentIssue(document.Path, FirstLine, IssueCodes.FrontMatterMissing, "Start the file with front-matter between '---' lines.")];
    }
}
