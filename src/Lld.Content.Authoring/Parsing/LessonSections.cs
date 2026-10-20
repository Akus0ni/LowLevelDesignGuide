namespace Lld.Content.Authoring.Parsing;

/// <summary>A <c>##</c> section of a lesson: from its heading to the next <c>##</c> heading.</summary>
/// <param name="Line">1-based line of the heading.</param>
/// <param name="EndLine">1-based line of the next <c>##</c> heading (or past the end); not part of the section.</param>
public sealed record LessonSection(string Heading, int Line, int EndLine)
{
    public bool Contains(int line) => line > Line && line < EndLine;
}

/// <summary>
/// The fixed sections of a <c>pattern</c> lesson (spec 006 §3.2), in reading order. The reader and the compare view
/// find *The idea* and *Structure* by these headings too (<c>patternFacts.ts</c>).
/// </summary>
public static class LessonSections
{
    public const string Idea = "The idea";
    public const string Structure = "Structure";
    public const string SeeIt = "See it";
    public const string ModernCSharp = "Modern C#";
    public const string SeenInDotNet = "Seen in .NET";
    public const string TravelsWith = "Travels with";
    public const string InThisApp = "In this app";

    // Diagram lessons (spec 008 §3.2).
    public const string Notation = "Notation";
    public const string ReadIt = "Read it";
    public const string CodeUml = "Code ↔ UML";
    public const string FromRequirements = "From requirements";
    public const string InAnInterview = "In an interview";

    // The interview lesson of a diagram module replaces Notation and Read it with these two.
    public const string WhichDiagram = "Which diagram for which question";
    public const string WorkedExample = "Worked example";

    private const int SectionLevel = 2;

    /// <summary>Sections every pattern lesson has.</summary>
    public static readonly IReadOnlyList<string> PatternRequired = [Idea, Structure, SeeIt, ModernCSharp, SeenInDotNet, TravelsWith];

    /// <summary>Every section a pattern lesson may name, in the order they must appear.</summary>
    public static readonly IReadOnlyList<string> PatternOrder =
        [.. PatternRequired, InThisApp, InterviewQuestions.Heading, FoldedSections.GoDeeper];

    public static IReadOnlyList<LessonSection> Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var headings = DocumentHeadings.Of(source).Where(h => h.Level == SectionLevel).ToList();
        return [.. headings.Select((heading, i) => new LessonSection(
            heading.Text,
            heading.Line,
            i + 1 < headings.Count ? headings[i + 1].Line : int.MaxValue))];
    }

    public static LessonSection? Find(IEnumerable<LessonSection> sections, string heading) =>
        sections.FirstOrDefault(s => string.Equals(s.Heading, heading, StringComparison.OrdinalIgnoreCase));
}
