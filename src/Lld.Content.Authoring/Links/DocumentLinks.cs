using Lld.Content.Authoring.Parsing;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Lld.Content.Authoring.Links;

/// <summary>A link destination as written in the file.</summary>
/// <param name="Start">Index of the destination in the file text.</param>
/// <param name="Length">Length of the destination (0 for <c>[text]()</c>).</param>
/// <param name="Line">1-based line.</param>
public sealed record DocumentLink(string Url, int Start, int Length, int Line, bool IsImage);

/// <summary>
/// Every link destination in a document: inline links <c>[text](url)</c> and reference definitions <c>[id]: url</c>.
/// Reference-style uses (<c>[text][id]</c>) are skipped because their destination is the definition, listed once.
/// Bare autolinks (<c>&lt;https://…&gt;</c>, <c>www.…</c>) are always external and are left out.
/// </summary>
public static class DocumentLinks
{
    public static IReadOnlyList<DocumentLink> Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var inline = source.Document.Descendants<LinkInline>()
            .Where(link => link.Reference is null && !link.IsAutoLink)
            .Select(link => Create(source, link.Url, link.UrlSpan, link.Span, link.IsImage));
        var definitions = source.Document.Descendants<LinkReferenceDefinition>()
            .Select(definition => Create(source, definition.Url, definition.UrlSpan, definition.Span, isImage: false));
        return [.. inline.Concat(definitions).OrderBy(link => link.Start)];
    }

    private static DocumentLink Create(SourceDocument source, string? url, SourceSpan urlSpan, SourceSpan whole, bool isImage)
    {
        var (start, length) = urlSpan.IsEmpty ? (whole.Start, 0) : (urlSpan.Start, urlSpan.Length);
        return new DocumentLink(url ?? string.Empty, start, length, source.LineAt(start), isImage);
    }
}
