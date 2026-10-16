using System.Text;
using Lld.Contracts;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line in the file.</param>
public sealed record DocumentHeading(int Level, string Text, string Anchor, int Line);

/// <summary>Every heading of a document with its anchor, and the h2/h3 outline the reader shows.</summary>
public static class DocumentHeadings
{
    private const int OutlineMinLevel = 2;
    private const int OutlineMaxLevel = 3;

    /// <summary>All headings in document order; repeats are numbered across the whole document, as on GitHub.</summary>
    public static IReadOnlyList<DocumentHeading> Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var slugger = new HeadingSlugger();
        return [.. source.Document.Descendants<HeadingBlock>().Select(heading =>
        {
            var text = PlainText(heading.Inline);
            return new DocumentHeading(heading.Level, text, slugger.Slug(text), source.LineAt(heading.Span.Start));
        })];
    }

    /// <summary>h2 and h3 headings, except those inside folded sections (interview questions, Go deeper).</summary>
    public static IReadOnlyList<OutlineEntry> Outline(IEnumerable<DocumentHeading> headings)
    {
        ArgumentNullException.ThrowIfNull(headings);
        var outline = new List<OutlineEntry>();
        var inFolded = false;
        foreach (var heading in headings.Where(h => h.Level is >= OutlineMinLevel and <= OutlineMaxLevel))
        {
            if (heading.Level == OutlineMinLevel)
            {
                inFolded = FoldedSections.HidesSubheadings(heading.Level, heading.Text);
            }
            else if (inFolded)
            {
                continue;
            }

            outline.Add(new OutlineEntry(heading.Level, heading.Text, heading.Anchor));
        }

        return outline;
    }

    /// <summary>The heading's text as a reader sees it: code, emphasis and link text kept, images and markup dropped.</summary>
    public static string PlainText(ContainerInline? inline)
    {
        var text = new StringBuilder();
        Append(inline, text);
        return text.ToString().Trim();
    }

    private static void Append(Inline? inline, StringBuilder text)
    {
        switch (inline)
        {
            case LinkInline { IsImage: true }:
                return;
            case ContainerInline container:
                foreach (var child in container)
                {
                    Append(child, text);
                }

                return;
            case LiteralInline literal:
                text.Append(literal.Content.AsSpan());
                return;
            case CodeInline code:
                text.Append(code.Content);
                return;
            case HtmlEntityInline entity:
                text.Append(entity.Transcoded.AsSpan());
                return;
            case LineBreakInline:
                text.Append(' ');
                return;
            case AutolinkInline autolink:
                text.Append(autolink.Url);
                return;
        }
    }
}
