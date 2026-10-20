using System.Text.RegularExpressions;
using Markdig.Syntax;

namespace Lld.Content.Authoring.Parsing;

/// <summary>A blockquote that starts with <c>[!TYPE]</c> (GitHub's alert syntax, spec 004 §3.5).</summary>
/// <param name="Line">1-based line of the marker.</param>
public sealed record Callout(string Type, int Line);

public static partial class Callouts
{
    /// <summary>The types the reader knows; the web app keeps the same list.</summary>
    public static readonly IReadOnlyList<string> Types = ["REMEMBER", "NOTE", "TIP", "INTERVIEW", "SMELL", "PITFALL", "APP", "WORK", "OVERUSE", "MISTAKES"];

    public const string Remember = "REMEMBER";

    /// <summary>"When it hurts" (spec 006 §3.8): drawn as a stamp in the warning colour.</summary>
    public const string Overuse = "OVERUSE";

    /// <summary>"Common mistakes" (spec 008 §3.7): drawn like the Overuse stamp.</summary>
    public const string Mistakes = "MISTAKES";

    public static IReadOnlyList<Callout> Of(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return [.. source.Document.Descendants<QuoteBlock>()
            .Select(quote => quote.FirstOrDefault() as ParagraphBlock)
            .OfType<ParagraphBlock>()
            .Select(paragraph => (Paragraph: paragraph, Match: Marker().Match(FirstLine(source, paragraph))))
            .Where(found => found.Match.Success)
            .Select(found => new Callout(found.Match.Groups["type"].Value, source.LineAt(found.Paragraph.Span.Start)))];
    }

    private static string FirstLine(SourceDocument source, ParagraphBlock paragraph)
    {
        var end = source.Text.IndexOf('\n', paragraph.Span.Start);
        return source.Text[paragraph.Span.Start..(end < 0 ? source.Text.Length : end)];
    }

    [GeneratedRegex(@"^\[!(?<type>[A-Za-z-]+)\]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex Marker();
}
