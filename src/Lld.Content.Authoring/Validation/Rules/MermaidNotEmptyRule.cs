using Markdig.Syntax;

namespace Lld.Content.Authoring.Validation.Rules;

/// <summary>A <c>```mermaid</c> block has a diagram in it. Diagram syntax itself is checked when it renders.</summary>
public sealed class MermaidNotEmptyRule : IContentRule
{
    public const string MermaidLanguage = "mermaid";

    public IEnumerable<ContentIssue> Check(ContentContext context) =>
        from document in context.Documents
        from block in document.Document.Descendants<FencedCodeBlock>()
        where string.Equals(block.Info, MermaidLanguage, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(block.Lines.ToString())
        select new ContentIssue(document.Path, document.LineAt(block.Span.Start), IssueCodes.MermaidEmpty, "This mermaid block is empty.");
}
