namespace Lld.Content.Authoring.Parsing;

/// <summary>
/// Heading anchors exactly as GitHub (and the reader's <c>github-slugger</c>) make them: lower-case, punctuation
/// removed, spaces → <c>-</c>, repeats numbered <c>-1</c>, <c>-2</c>. One instance per document, because repeats
/// count across the whole document. Checked against <c>tests/fixtures/content/slugs.json</c>.
/// </summary>
public sealed class HeadingSlugger
{
    private readonly Dictionary<string, int> _occurrences = new(StringComparer.Ordinal);

    public string Slug(string text)
    {
        var original = Normalize(text);
        var slug = original;
        while (_occurrences.ContainsKey(slug))
        {
            var count = ++_occurrences[original];
            slug = $"{original}-{count}";
        }

        _occurrences[slug] = 0;
        return slug;
    }

    /// <summary>The anchor of one heading, ignoring repeats.</summary>
    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
#pragma warning disable CA1308 // GitHub anchors are lower-case by definition.
        var lower = text.ToLowerInvariant();
#pragma warning restore CA1308
        return GithubSluggerPattern.Removed().Replace(lower, string.Empty).Replace(' ', '-');
    }
}
