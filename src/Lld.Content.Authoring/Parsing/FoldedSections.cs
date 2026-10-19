namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// Lesson sections the reader folds (spec 004 §3.2, §3.5): the interview questions and "Go deeper". Their sub-headings
/// stay out of the outline, and "Go deeper" stays out of the reading time (it is optional reading).
/// </summary>
public static class FoldedSections
{
    public const string GoDeeper = "Go deeper";

    private const int SectionLevel = 2;

    /// <summary>A <c>##</c> heading whose sub-headings the outline leaves out.</summary>
    public static bool HidesSubheadings(int level, string text) =>
        level == SectionLevel && (Is(text, InterviewQuestions.Heading) || Is(text, GoDeeper));

    /// <summary>A <c>##</c> heading whose section doesn't count towards reading time.</summary>
    public static bool IsOptionalReading(int level, string text) => level == SectionLevel && Is(text, GoDeeper);

    private static bool Is(string text, string heading) => string.Equals(text, heading, StringComparison.OrdinalIgnoreCase);
}
