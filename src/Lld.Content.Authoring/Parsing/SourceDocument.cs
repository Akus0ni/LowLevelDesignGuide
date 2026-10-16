using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <summary>A parsed <c>lesson.md</c> or <c>cheatsheet.md</c>: front-matter, Markdown syntax tree and the body text.</summary>
public sealed class SourceDocument
{
    private readonly int[] _lineStarts;

    private SourceDocument(string path, string text, YamlMap? frontMatter, MarkdownDocument document, int bodyStart)
    {
        Path = path;
        Text = text;
        FrontMatter = frontMatter;
        Document = document;
        BodyStart = bodyStart;
        _lineStarts = LineStarts(text);
    }

    /// <summary>Repository-relative path, e.g. <c>content/case-studies/auth/lesson.md</c>.</summary>
    public string Path { get; }

    /// <summary>The whole file, front-matter included; syntax-tree spans point into it.</summary>
    public string Text { get; }

    /// <summary><c>null</c> when the file has no front-matter or it could not be read.</summary>
    public YamlMap? FrontMatter { get; }

    public MarkdownDocument Document { get; }

    /// <summary>Index in <see cref="Text"/> where the Markdown after the front-matter starts.</summary>
    public int BodyStart { get; }

    public string Body => Text[BodyStart..];

    /// <summary>1-based line of a character index in <see cref="Text"/>.</summary>
    public int LineAt(int index)
    {
        var found = Array.BinarySearch(_lineStarts, index);
        return (found >= 0 ? found : ~found - 1) + 1;
    }

    /// <summary>Parses a file; a front-matter syntax error is returned as an issue and leaves <see cref="FrontMatter"/> null.</summary>
    public static (SourceDocument Document, ContentIssue? Issue) Parse(string path, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var document = MarkdownParsing.Parse(text);
        var block = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (block is null)
        {
            return (new SourceDocument(path, text, null, document, 0), null);
        }

        // Markdig lines are 0-based and the block starts at the opening "---": the YAML begins one line later.
        var yamlFirstLine = block.Line + 2;
        var (map, issue) = YamlMapParser.Parse(block.Lines.ToString(), path, yamlFirstLine);
        return (new SourceDocument(path, text, map, document, SkipNewLine(text, block.Span.End + 1)), issue);
    }

    private static int SkipNewLine(string text, int index)
    {
        if (index < text.Length && text[index] == '\r')
        {
            index++;
        }

        if (index < text.Length && text[index] == '\n')
        {
            index++;
        }

        return index;
    }

    private static int[] LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                starts.Add(i + 1);
            }
        }

        return [.. starts];
    }
}
