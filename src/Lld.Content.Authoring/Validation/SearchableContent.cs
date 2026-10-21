using System.Globalization;
using System.Text.RegularExpressions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Authoring.Validation;

/// <summary>
/// Every section's search text as lower-case words, for the vocabulary rules (spec 009 §3.3). Matching here is plain
/// word matching (no stemming), stricter than search itself, so a word that passes here is always found.
/// </summary>
public sealed partial class SearchableContent
{
    private readonly IReadOnlyList<string[]> _sections;

    public SearchableContent(IEnumerable<SourceDocument> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);
        _sections = [.. documents.SelectMany(SearchText.Sections).Select(s => Words($"{s.Heading} {s.Text}"))];
    }

    /// <summary>Lower-case words as search splits them: runs of letters, digits and marks.</summary>
    public static string[] Words(string text) =>
        [.. WordPattern().Matches(text ?? string.Empty).Select(m => m.Value.ToLower(CultureInfo.InvariantCulture))];

    /// <summary>The words appear next to each other, in this order, in some section.</summary>
    public bool ContainsPhrase(IReadOnlyList<string> words) =>
        words.Count > 0 && _sections.Any(section => IndexOf(section, words) >= 0);

    /// <summary>
    /// Some section holds every word, the last one possibly as the start of a word: what a search for this text finds
    /// while the learner is still typing it (spec §3.2).
    /// </summary>
    public bool FindsAll(IReadOnlyList<string> words) =>
        words.Count > 0 && _sections.Any(section =>
            words.SkipLast(1).All(word => section.Contains(word, StringComparer.Ordinal))
            && section.Any(word => word.StartsWith(words[^1], StringComparison.Ordinal)));

    private static int IndexOf(string[] section, IReadOnlyList<string> words)
    {
        for (var start = 0; start + words.Count <= section.Length; start++)
        {
            var matches = true;
            for (var i = 0; i < words.Count && matches; i++)
            {
                matches = string.Equals(section[start + i], words[i], StringComparison.Ordinal);
            }

            if (matches)
            {
                return start;
            }
        }

        return -1;
    }

    [GeneratedRegex(@"[\p{L}\p{N}\p{M}]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
