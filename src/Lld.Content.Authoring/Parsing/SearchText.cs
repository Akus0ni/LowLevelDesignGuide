using System.Text;
using System.Text.RegularExpressions;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// The plain text search indexes, one entry per <c>##</c> section (spec 009 §3.1): headings, paragraphs, lists, tables
/// and callouts (folded ones too) as a reader sees them. Code blocks, diagrams, HTML, link URLs and images are left out,
/// and callout markers such as <c>[!REMEMBER]</c> are dropped. The text before the first <c>##</c> is the intro
/// (anchor <c>""</c>); the <c>#</c> title is left out, as search keeps the title apart. Anchors are the outline's, so
/// a hit opens the right heading.
/// </summary>
public static partial class SearchText
{
    private const int SectionLevel = 2;

    public static IReadOnlyList<SearchSection> Sections(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var sectionHeadings = DocumentHeadings.Of(source).Where(h => h.Level == SectionLevel).ToDictionary(h => h.Line);
        var sections = new List<SearchSection>();
        var anchor = string.Empty;
        var heading = string.Empty;
        var text = new StringBuilder();

        void Flush()
        {
            var plain = Tidy(text.ToString());
            if (anchor.Length > 0 || plain.Length > 0)
            {
                sections.Add(new SearchSection(anchor, heading, plain));
            }

            text.Clear();
        }

        foreach (var block in source.Document)
        {
            if (block is HeadingBlock { Level: SectionLevel } && sectionHeadings.TryGetValue(source.LineAt(block.Span.Start), out var next))
            {
                Flush();
                (anchor, heading) = (next.Anchor, next.Text);
                continue;
            }

            Append(block, text);
        }

        Flush();
        return sections;
    }

    private static void Append(Block block, StringBuilder text)
    {
        switch (block)
        {
            // The h1 is the title, which search already holds in its own (weighted) column.
            case YamlFrontMatterBlock or CodeBlock or HtmlBlock or HeadingBlock { Level: 1 }:
                return;
            case LeafBlock leaf:
                text.Append(DocumentHeadings.PlainText(leaf.Inline)).Append(' ');
                return;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    Append(child, text);
                }

                return;
        }
    }

    private static string Tidy(string text) => Spaces().Replace(CalloutMarker().Replace(text, " "), " ").Trim();

    [GeneratedRegex(@"\[![A-Za-z-]+\]", RegexOptions.CultureInvariant)]
    private static partial Regex CalloutMarker();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Spaces();
}
