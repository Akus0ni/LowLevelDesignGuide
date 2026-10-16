using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line of the question's heading.</param>
public sealed record InterviewQuestion(string Text, int Line, bool HasAnswer);

/// <param name="Line">1-based line of the section heading.</param>
public sealed record InterviewSection(int Line, IReadOnlyList<InterviewQuestion> Questions);

/// <summary>
/// A lesson's <c>## Interview questions</c> section (spec 004 §3.5): each <c>###</c> heading is a question and the
/// blocks below it, up to the next heading, are its answer. The reader folds the answers.
/// </summary>
public static class InterviewQuestions
{
    public const string Heading = "Interview questions";

    private const int SectionLevel = 2;
    private const int QuestionLevel = 3;

    public static bool IsSection(DocumentHeading heading)
    {
        ArgumentNullException.ThrowIfNull(heading);
        return IsSection(heading.Level, heading.Text);
    }

    /// <summary>The section, or <c>null</c> when the lesson has none.</summary>
    public static InterviewSection? Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var blocks = source.Document.ToList();
        var start = blocks.FindIndex(block => block is HeadingBlock heading && IsSection(heading.Level, DocumentHeadings.PlainText(heading.Inline)));
        if (start < 0)
        {
            return null;
        }

        var questions = new List<InterviewQuestion>();
        for (var i = start + 1; i < blocks.Count && !IsHeadingAtMost(blocks[i], SectionLevel); i++)
        {
            if (blocks[i] is HeadingBlock { Level: QuestionLevel } question)
            {
                var hasAnswer = i + 1 < blocks.Count && !IsHeadingAtMost(blocks[i + 1], QuestionLevel);
                questions.Add(new InterviewQuestion(DocumentHeadings.PlainText(question.Inline), source.LineAt(question.Span.Start), hasAnswer));
            }
        }

        return new InterviewSection(source.LineAt(blocks[start].Span.Start), questions);
    }

    private static bool IsSection(int level, string text) =>
        level == SectionLevel && string.Equals(text, Heading, StringComparison.OrdinalIgnoreCase);

    private static bool IsHeadingAtMost(Block block, int level) => block is HeadingBlock heading && heading.Level <= level;
}
