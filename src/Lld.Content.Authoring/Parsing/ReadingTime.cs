using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lld.Content.Authoring.Parsing;

/// <summary>Minutes to read the prose of a document (code blocks and diagrams are skimmed, so they don't count).</summary>
public static class ReadingTime
{
    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n'];

    public static int Minutes(MarkdownDocument document, int wordsPerMinute)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(wordsPerMinute);
        var words = document.Descendants<LeafBlock>()
            .Where(block => block is not CodeBlock)
            .Sum(block => CountWords(block.Inline));
        return Math.Max(1, (int)Math.Ceiling(words / (double)wordsPerMinute));
    }

    public static int CountWords(ContainerInline? inline) =>
        inline is null
            ? 0
            : inline.Descendants<LeafInline>().Sum(leaf => leaf switch
            {
                LiteralInline literal => Count(literal.Content.ToString()),
                CodeInline code => Count(code.Content),
                _ => 0,
            });

    private static int Count(string text) => text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
}
