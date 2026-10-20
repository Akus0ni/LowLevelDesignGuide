using System.Text.RegularExpressions;

namespace Lld.Domain.Notes;

/// <summary>
/// Reads the <c>#tags</c> a learner wrote in a note (spec 007 §3.5): <c>#</c>, a letter, then up to 29 letters, digits or
/// hyphens. Not tags: anything in code, Markdown headings (<c># Title</c>), URL fragments, HTML entities and <c>##</c>.
/// Results are lower case, without repeats, in the order they first appear. The web's <c>tags.ts</c> follows the same
/// rule; both are checked against <c>tests/fixtures/notes/tags.json</c>.
/// </summary>
public static partial class TagParser
{
    public static IReadOnlyList<string> Parse(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var text = InlineCode().Replace(FencedCode().Replace(body.ReplaceLineEndings("\n"), " "), " ");
        return [.. Tag().Matches(text).Select(match => match.Groups[1].Value.ToLowerInvariant()).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>A fenced block: an opening fence of 3+ backticks or tildes up to the matching close (or the end).</summary>
    [GeneratedRegex(@"^[ ]{0,3}(`{3,}|~{3,})[^\n]*\n[\s\S]*?(?:^[ ]{0,3}\1[ \t]*$|\z)", RegexOptions.Multiline)]
    private static partial Regex FencedCode();

    /// <summary>A code span: a run of backticks up to the same run.</summary>
    [GeneratedRegex(@"(`+)[\s\S]*?\1")]
    private static partial Regex InlineCode();

    [GeneratedRegex(@"(?<![A-Za-z0-9_/#&])#([A-Za-z][A-Za-z0-9-]{0,29})(?![A-Za-z0-9_-])")]
    private static partial Regex Tag();
}
