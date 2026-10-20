using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// A diagram lesson has the fixed sections of spec 008 §3.2: The idea and In an interview always; then either Notation,
/// Read it and one of Code ↔ UML / From requirements (a diagram lesson), or Which diagram for which question and
/// Worked example (the interview lesson). Missing sections are reported at line 1.
/// </summary>
public sealed class DiagramSectionsRule : IContentRule
{
    private const int FirstLine = 1;

    private static readonly string[] Always = [LessonSections.Idea, LessonSections.InAnInterview];
    private static readonly string[] DiagramParts = [LessonSections.Notation, LessonSections.ReadIt];
    private static readonly string[] InterviewParts = [LessonSections.WhichDiagram, LessonSections.WorkedExample];

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.DiagramLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var sections = LessonSections.Of(lesson);
        bool Has(string heading) => LessonSections.Find(sections, heading) is not null;

        var isInterviewLesson = InterviewParts.Any(Has);
        var required = Always.Concat(isInterviewLesson ? InterviewParts : DiagramParts);
        foreach (var missing in required.Where(h => !Has(h)))
        {
            yield return Missing(lesson, $"'## {missing}'");
        }

        if (!isInterviewLesson && !Has(LessonSections.CodeUml) && !Has(LessonSections.FromRequirements))
        {
            yield return Missing(lesson, $"a '## {LessonSections.CodeUml}' or '## {LessonSections.FromRequirements}'");
        }
    }

    private static ContentIssue Missing(SourceDocument lesson, string what) =>
        new(lesson.Path, FirstLine, IssueCodes.DiagramSectionMissing, $"Diagram lessons need {what} section.");
}

/// <summary>
/// The sections of a diagram lesson that show a diagram really draw one: Notation, Read it, From requirements and
/// Worked example hold a <c>mermaid</c> fence; Code ↔ UML holds a <c>mermaid</c> fence paired with code
/// (<c>code="…"</c>, spec 008 §3.4). Reported at the section heading.
/// </summary>
public sealed class DiagramSectionDiagramRule : IContentRule
{
    private const string Mermaid = "mermaid";

    private static readonly string[] NeedADiagram =
        [LessonSections.Notation, LessonSections.ReadIt, LessonSections.FromRequirements, LessonSections.WorkedExample];

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.DiagramLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var sections = LessonSections.Of(lesson);
        var diagrams = CodeFences.Of(lesson).Where(IsMermaid).ToList();
        foreach (var heading in NeedADiagram)
        {
            if (LessonSections.Find(sections, heading) is { } section && !diagrams.Any(f => section.Contains(f.Line)))
            {
                yield return new(lesson.Path, section.Line, IssueCodes.DiagramSectionNoDiagram,
                    $"'## {heading}' needs a ```mermaid diagram.");
            }
        }

        if (LessonSections.Find(sections, LessonSections.CodeUml) is { } pair
            && !diagrams.Any(f => pair.Contains(f.Line) && f.Info[FenceInfo.CodeAttribute] is not null))
        {
            yield return new(lesson.Path, pair.Line, IssueCodes.DiagramSectionNoDiagram,
                $"'## {LessonSections.CodeUml}' needs a ```mermaid fence with code=\"path#Type\" naming the C# it draws.");
        }
    }

    private static bool IsMermaid(CodeFence fence) =>
        string.Equals(fence.Info.Language, Mermaid, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Every diagram lesson has exactly one Mistakes stamp, "common mistakes" (spec 008 §3.7).</summary>
public sealed class MistakesStampRule : IContentRule
{
    private const int FirstLine = 1;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.DiagramLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var stamps = Callouts.Of(lesson).Where(c => c.Type == Callouts.Mistakes).ToList();
        if (stamps.Count == 0)
        {
            yield return new(lesson.Path, FirstLine, IssueCodes.MistakesMissing,
                "Add one '> [!MISTAKES]' stamp: 2–3 common mistakes when drawing this diagram.");
        }

        foreach (var extra in stamps.Skip(1))
        {
            yield return new(lesson.Path, extra.Line, IssueCodes.MistakesDuplicate, "Write one '[!MISTAKES]' stamp per lesson.");
        }
    }
}
