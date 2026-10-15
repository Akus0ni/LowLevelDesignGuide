using System.Text.RegularExpressions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>Lesson ids are unique: progress, notes and bookmarks are stored against them.</summary>
public sealed class LessonIdUniqueRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        LessonIds.Of(context)
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1).Select(x => new ContentIssue(
                x.Document.Path, x.Line, IssueCodes.IdDuplicate, $"Id '{x.Id}' is already used by {group.First().Document.Path}.")));
}

/// <summary>Lesson ids and module / lesson folder names are kebab-case, because they end up in routes and storage keys.</summary>
public sealed partial class IdentifierFormatRule : IContentRule
{
    private const string Expected = "lower-case letters, digits and single dashes (kebab-case)";

    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        var ids = LessonIds.Of(context)
            .Where(x => !IsKebabCase(x.Id))
            .Select(x => new ContentIssue(x.Document.Path, x.Line, IssueCodes.IdFormat, $"Id '{x.Id}' must use {Expected}."));
        var modules = context.Set.Modules
            .Where(m => !IsKebabCase(m.Slug))
            .Select(m => new ContentIssue(m.Path, null, IssueCodes.SlugFormat, $"Folder name '{m.Slug}' must use {Expected}."));
        var lessons = context.Lessons
            .Where(l => !IsKebabCase(l.Slug))
            .Select(l => new ContentIssue(l.Path, null, IssueCodes.SlugFormat, $"Folder name '{l.Slug}' must use {Expected}."));
        return ids.Concat(modules).Concat(lessons);
    }

    public static bool IsKebabCase(string value) => KebabCase().IsMatch(value);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCase();
}

/// <summary>No two modules share an <c>order</c>, and no two lessons in one module do, so the reading order is clear.</summary>
public sealed class OrderUniqueRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context)
    {
        var modules = Duplicates(context.Set.Modules
            .Where(m => m.Manifest is not null)
            .Select(m => (Map: m.Manifest!, Path: m.ManifestPath)), "module");
        var lessons = context.Set.Modules.SelectMany(m => Duplicates(
            m.Lessons
                .Where(l => l.Lesson?.FrontMatter is not null)
                .Select(l => (Map: l.Lesson!.FrontMatter!, Path: l.LessonPath)),
            $"lesson in {m.Slug}"));
        return modules.Concat(lessons);
    }

    private static IEnumerable<ContentIssue> Duplicates(IEnumerable<(YamlMap Map, string Path)> items, string what) =>
        items
            .Select(x => (x.Path, Entry: x.Map[FrontMatterKeys.Order]))
            .Where(x => x.Entry is not null && FrontMatterSchema.TryParseOrder(x.Entry.Scalar, out _))
            .GroupBy(x => x.Entry!.Scalar, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1).Select(x => new ContentIssue(
                x.Path, x.Entry!.Line, IssueCodes.OrderDuplicate, $"Another {what} already has order {group.Key} ({group.First().Path}).")));
}

internal static class LessonIds
{
    public static IEnumerable<(string Id, int Line, SourceDocument Document)> Of(ContentContext context) =>
        context.Lessons
            .Select(l => l.Lesson)
            .OfType<SourceDocument>()
            .Select(d => (Document: d, Entry: d.FrontMatter?[FrontMatterKeys.Id]))
            .Where(x => x.Entry is { Kind: YamlValueKind.Scalar, Scalar.Length: > 0 })
            .Select(x => (x.Entry!.Scalar!, x.Entry.Line, x.Document));
}
