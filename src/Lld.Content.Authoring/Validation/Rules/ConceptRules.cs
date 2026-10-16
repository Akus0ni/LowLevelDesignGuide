using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>Callouts use a type the reader knows (<see cref="Callouts.Types"/>).</summary>
public sealed class CalloutTypeRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from callout in Callouts.Of(document)
        where !Callouts.Types.Contains(callout.Type, StringComparer.Ordinal)
        select new ContentIssue(document.Path, callout.Line, IssueCodes.CalloutUnknownType,
            $"Unknown callout '[!{callout.Type}]'. Use one of: {string.Join(", ", Callouts.Types)}.");
}

/// <summary>A concept lesson ends with 5–8 interview questions, each with an answer (spec 004 §3.2).</summary>
public sealed class InterviewQuestionsRule : IContentRule
{
    public const int MinQuestions = 5;
    public const int MaxQuestions = 8;

    private const int FirstLine = 1;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.ConceptLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        if (InterviewQuestions.Of(lesson) is not { } section)
        {
            yield return new(lesson.Path, FirstLine, IssueCodes.InterviewMissing, $"Concept lessons end with a '## {InterviewQuestions.Heading}' section.");
            yield break;
        }

        if (section.Questions.Count is < MinQuestions or > MaxQuestions)
        {
            yield return new(lesson.Path, section.Line, IssueCodes.InterviewCount,
                $"Write {MinQuestions}–{MaxQuestions} questions as '###' headings; found {section.Questions.Count}.");
        }

        foreach (var question in section.Questions.Where(q => !q.HasAnswer))
        {
            yield return new(lesson.Path, question.Line, IssueCodes.InterviewNoAnswer, $"'{question.Text}' has no answer below it.");
        }
    }
}

/// <summary>Concept lessons and their cheat sheets quote all C# from compiled code (PRD §7, spec 004 §3.3).</summary>
public sealed class ConceptCodeFromSnippetsRule : IContentRule
{
    private static readonly HashSet<string> CSharp = new(StringComparer.OrdinalIgnoreCase) { "csharp", "cs", "c#" };

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from lesson in context.ConceptLessons
        from document in new[] { lesson.Lesson, lesson.CheatSheet }.OfType<SourceDocument>()
        from fence in CodeFences.Of(document)
        where CSharp.Contains(fence.Info.Language) && fence.Snippet is null
        select new ContentIssue(document.Path, fence.Line, IssueCodes.CodeInlineCSharp,
            "C# in this module must compile: put it in samples/ and quote it with snippet=\"path#Symbol\".");
}

/// <summary>
/// A module map (<c>map:</c> in <c>module.yml</c>, spec 004 §3.6) is a flowchart whose nodes are exactly the module's
/// lessons. The map is written as a block (<c>map: |</c>), so its line N is file line <c>map:</c> + N.
/// </summary>
public sealed class ModuleMapRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.Set.Modules.SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceModule module)
    {
        if (module.Manifest?[FrontMatterKeys.Map] is not { Scalar: { } map } entry)
        {
            yield break;
        }

        if (!ModuleMap.IsFlowchart(map))
        {
            yield return new(module.ManifestPath, entry.Line, IssueCodes.MapNotFlowchart, "The map must be a Mermaid flowchart ('flowchart LR' or 'flowchart TD').");
            yield break;
        }

        var slugs = module.Lessons.Select(l => l.Slug).ToHashSet(StringComparer.Ordinal);
        var nodes = ModuleMap.Nodes(map);
        foreach (var node in nodes.Where(n => !slugs.Contains(n.Id)))
        {
            yield return new(module.ManifestPath, entry.Line + node.Line, IssueCodes.MapUnknownNode,
                $"'{node.Id}' isn't a lesson in this module (node ids are lesson folder names; put spaces around arrows).");
        }

        foreach (var slug in slugs.Except(nodes.Select(n => n.Id)).Order(StringComparer.Ordinal))
        {
            yield return new(module.ManifestPath, entry.Line, IssueCodes.MapLessonMissing, $"Lesson '{slug}' isn't on the map.");
        }
    }
}
