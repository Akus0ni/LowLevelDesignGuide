using System.Text.RegularExpressions;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Reading;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>
/// A pattern lesson has the fixed sections of spec 006 §3.2, in order: The idea, Structure, See it, Modern C#,
/// Seen in .NET, Travels with, then the optional In this app, the interview questions and Go deeper. Missing sections
/// are reported at line 1, the first misplaced one at its heading.
/// </summary>
public sealed class PatternSectionsRule : IContentRule
{
    private const int FirstLine = 1;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.PatternLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var sections = LessonSections.Of(lesson);
        foreach (var missing in LessonSections.PatternRequired.Where(h => LessonSections.Find(sections, h) is null))
        {
            yield return new(lesson.Path, FirstLine, IssueCodes.PatternSectionMissing, $"Pattern lessons need a '## {missing}' section.");
        }

        var furthest = -1;
        foreach (var section in sections)
        {
            var position = IndexOf(section.Heading);
            if (position < 0)
            {
                continue;
            }

            if (position < furthest)
            {
                // One issue is enough: once a section is misplaced, everything after it looks misplaced too.
                yield return new(lesson.Path, section.Line, IssueCodes.PatternSectionOrder,
                    $"'## {section.Heading}' is out of order. Write: {string.Join(", ", LessonSections.PatternOrder)}.");
                yield break;
            }

            furthest = Math.Max(furthest, position);
        }
    }

    private static int IndexOf(string heading)
    {
        for (var i = 0; i < LessonSections.PatternOrder.Count; i++)
        {
            if (string.Equals(LessonSections.PatternOrder[i], heading, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>The Structure section of a pattern lesson draws the classic UML class diagram (Mermaid <c>classDiagram</c>).</summary>
public sealed class PatternStructureRule : IContentRule
{
    private const string Mermaid = "mermaid";
    private const string ClassDiagram = "classDiagram";

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.PatternLessons.Select(l => l.Lesson).OfType<SourceDocument>()
        let structure = LessonSections.Find(LessonSections.Of(document), LessonSections.Structure)
        where structure is not null && !CodeFences.Of(document).Any(f => structure.Contains(f.Line) && IsClassDiagram(f))
        select new ContentIssue(document.Path, structure.Line, IssueCodes.PatternStructureNoClassDiagram,
            $"'## {LessonSections.Structure}' needs a ```mermaid fence that starts with '{ClassDiagram}'.");

    private static bool IsClassDiagram(CodeFence fence) =>
        string.Equals(fence.Info.Language, Mermaid, StringComparison.OrdinalIgnoreCase)
        && fence.Code.TrimStart().StartsWith(ClassDiagram, StringComparison.Ordinal);
}

/// <summary>The See it section of a pattern lesson shows a Before / After pair: the problem code, then the pattern.</summary>
public sealed class PatternCompareRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.PatternLessons.Select(l => l.Lesson).OfType<SourceDocument>()
        let seeIt = LessonSections.Find(LessonSections.Of(document), LessonSections.SeeIt)
        where seeIt is not null && !CodeFences.Compare(CodeFences.Of(document)).Pairs.Any(p => seeIt.Contains(p.Before.Line))
        select new ContentIssue(document.Path, seeIt.Line, IssueCodes.PatternSeeItNoCompare,
            $"'## {LessonSections.SeeIt}' needs a 'before' fence followed by an 'after' fence.");
}

/// <summary>Every pattern lesson has exactly one Overuse stamp, "when it hurts" (spec 006 §3.8).</summary>
public sealed class OveruseStampRule : IContentRule
{
    private const int FirstLine = 1;

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.PatternLessons.Select(l => l.Lesson).OfType<SourceDocument>().SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceDocument lesson)
    {
        var stamps = Callouts.Of(lesson).Where(c => c.Type == Callouts.Overuse).ToList();
        if (stamps.Count == 0)
        {
            yield return new(lesson.Path, FirstLine, IssueCodes.OveruseMissing,
                "Add one '> [!OVERUSE]' stamp: 2–3 signs the pattern is doing harm.");
        }

        foreach (var extra in stamps.Skip(1))
        {
            yield return new(lesson.Path, extra.Line, IssueCodes.OveruseDuplicate, "Write one '[!OVERUSE]' stamp per lesson.");
        }
    }
}

/// <summary>
/// Each lesson of a pattern module has a periodic-table <c>symbol</c>: two letters, upper then lower case, unique in
/// the module (spec 006 §3.5).
/// </summary>
public sealed partial class SymbolRule : IContentRule
{
    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        context.PatternModules.SelectMany(Problems);

    private static IEnumerable<ContentIssue> Problems(SourceModule module)
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        // In reading order, so the later lesson is the one told to change its symbol.
        foreach (var lesson in module.Lessons.Where(l => l.Lesson?.FrontMatter is not null).OrderBy(ReadingOrder))
        {
            var document = lesson.Lesson!;
            var front = document.FrontMatter!;
            if (front[FrontMatterKeys.Symbol] is not { Scalar: { } raw } entry)
            {
                yield return new(document.Path, front.Line, IssueCodes.SymbolMissing, "Add 'symbol:', two letters for the periodic table, e.g. symbol: St.");
                continue;
            }

            var symbol = raw.Trim();
            if (!Format().IsMatch(symbol))
            {
                yield return new(document.Path, entry.Line, IssueCodes.SymbolFormat, $"'symbol' is two letters, upper then lower case (e.g. 'St'), not '{symbol}'.");
            }
            else if (!seen.TryAdd(symbol, lesson.Slug))
            {
                yield return new(document.Path, entry.Line, IssueCodes.SymbolDuplicate, $"Symbol '{symbol}' is already used by '{seen[symbol]}'.");
            }
        }
    }

    private static int ReadingOrder(SourceLesson lesson) =>
        FrontMatterSchema.TryParseOrder(lesson.Lesson!.FrontMatter![FrontMatterKeys.Order]?.Scalar, out var order) ? order : int.MaxValue;

    [GeneratedRegex("^[A-Z][a-z]$", RegexOptions.CultureInvariant)]
    private static partial Regex Format();
}
