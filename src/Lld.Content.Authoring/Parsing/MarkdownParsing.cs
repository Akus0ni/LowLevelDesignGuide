using Markdig;
using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// The one Markdig pipeline used for content: GitHub-flavoured features the reader renders (remark-gfm) plus
/// YAML front-matter and precise source positions, so links can be rewritten in place.
/// </summary>
public static class MarkdownParsing
{
    public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .UsePreciseSourceLocation()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseAutoLinks()
        .Build();

    public static MarkdownDocument Parse(string text) => Markdown.Parse(text, Pipeline);
}
