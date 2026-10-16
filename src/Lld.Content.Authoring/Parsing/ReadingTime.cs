using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// Minutes to read the prose of a document. Code blocks and diagrams are skimmed, so they don't count; nor does an
/// optional "Go deeper" section (<see cref="FoldedSections"/>), which the reader opens only when they want more.
/// </summary>
public static class ReadingTime
{
    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n'];

    public static int Minutes(MarkdownDocument document, int wordsPerMinute)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(wordsPerMinute);
        var words = 0;
        var optional = false;
        foreach (var block in document)
        {
            if (block is HeadingBlock { Level: 2 } heading)
            {
                optional = FoldedSections.IsOptionalReading(heading.Level, DocumentHeadings.PlainText(heading.Inline));
            }

            if (!optional)
            {
                words += Words(block);
            }
        }

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

    private static int Words(Block block) => block switch
    {
        CodeBlock => 0,
        LeafBlock leaf => CountWords(leaf.Inline),
        ContainerBlock container => container.Descendants<LeafBlock>().Where(b => b is not CodeBlock).Sum(b => CountWords(b.Inline)),
        _ => 0,
    };

    private static int Count(string text) => text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
}
