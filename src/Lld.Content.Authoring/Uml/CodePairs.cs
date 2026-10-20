using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Snippets;

namespace Lld.Content.Authoring.Uml;

/// <summary>
/// A diagram fence with <c>code="…"</c> (spec 008 §3.4) and the code it names: <see cref="Code"/> when the reference
/// resolves, otherwise <see cref="Problem"/> says why not.
/// </summary>
public sealed record CodePair(CodeFence Diagram, string Reference, string? Code, string? Problem)
{
    public DiagramType Type => DiagramTypes.Of(Diagram.Code);

    /// <summary>1-based document line of a diagram source line (the fence's first code line is line 1).</summary>
    public int DocumentLine(int diagramLine) => Diagram.Line + diagramLine;
}

/// <summary>The Code ↔ UML pairs of a document: <c>mermaid</c> fences with <c>code=</c> (elsewhere the key is a fence error).</summary>
public static class CodePairs
{
    private const string Mermaid = "mermaid";

    public static IEnumerable<CodePair> Of(SourceDocument document, ICodeSnippetSource snippets)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(snippets);
        foreach (var fence in CodeFences.Of(document))
        {
            if (fence.Info[FenceInfo.CodeAttribute] is { } value
                && string.Equals(fence.Info.Language, Mermaid, StringComparison.OrdinalIgnoreCase))
            {
                yield return Resolve(fence, value, snippets);
            }
        }
    }

    private static CodePair Resolve(CodeFence fence, string value, ICodeSnippetSource snippets)
    {
        var (reference, problem) = SnippetReference.Parse(value);
        if (reference is null)
        {
            return new CodePair(fence, value, null, problem);
        }

        return snippets.Extract(reference) switch
        {
            SnippetResult.Found found => new CodePair(fence, value, found.Code, null),
            SnippetResult.FileMissing missing => new CodePair(fence, value, null, $"{missing.Path} doesn't exist."),
            SnippetResult.SymbolMissing symbol => new CodePair(fence, value, null, $"no type or member '{symbol.Selector}' in {reference.Path}."),
            SnippetResult.Ambiguous ambiguous =>
                new CodePair(fence, value, null, $"'{ambiguous.Selector}' matches {ambiguous.Count} declarations; qualify it (Outer.Inner)."),
            _ => new CodePair(fence, value, null, "it doesn't resolve."),
        };
    }
}
