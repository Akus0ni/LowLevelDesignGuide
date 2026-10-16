namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>Every module folder has a <c>module.yml</c>.</summary>
public sealed class ModuleManifestRequiredRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules
            .Where(m => m.Manifest is null && !context.Files.FileExists(m.ManifestPath))
            .Select(m => new ContentIssue(m.ManifestPath, null, IssueCodes.ModuleManifestMissing, "Every module needs a module.yml (title, order, summary)."));
}

/// <summary>Every lesson folder has a <c>lesson.md</c>.</summary>
public sealed class LessonRequiredRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Lessons
            .Where(l => l.Lesson is null)
            .Select(l => new ContentIssue(l.LessonPath, null, IssueCodes.LessonMissing, "Every lesson folder needs a lesson.md."));
}

/// <summary>Every lesson has a cheat sheet (PRD F3).</summary>
public sealed class CheatSheetRequiredRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Lessons
            .Where(l => l.CheatSheet is null)
            .Select(l => new ContentIssue(l.CheatSheetPath, null, IssueCodes.CheatSheetMissing, "Every lesson needs a cheatsheet.md for quick revision."));
}
