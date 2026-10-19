namespace Lld.Content.Authoring.Parsing;

/// <param name="Problem">Why the entry can't be read, or <c>null</c>.</param>
public sealed record ParsedMnemonic(string Word, IReadOnlyList<string> Lessons, string? Problem);

/// <summary>
/// A module mnemonic (spec 004 §3.8), written in <c>module.yml</c> as <c>"SOLID: srp, ocp, lsp, isp, dip"</c>: a word,
/// then one lesson slug per letter. Characters that aren't letters (<c>A-PIE</c>) are just decoration.
/// A phrase (spec 006 §3.4) spells with the first letter of each word: <c>"Smart Builders Always Follow Plans: ..."</c>.
/// The web splits words the same way (<c>mnemonicTiles.ts</c>); both are checked against
/// <c>tests/fixtures/content/mnemonics.json</c>.
/// </summary>
public static class Mnemonics
{
    private const char WordEnd = ':';
    private const char Separator = ',';

    public static ParsedMnemonic Parse(string entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var colon = entry.IndexOf(WordEnd, StringComparison.Ordinal);
        if (colon <= 0)
        {
            return new ParsedMnemonic(entry, [], $"'{entry}': write 'WORD: lesson, lesson, ...'.");
        }

        var word = entry[..colon].Trim();
        var lessons = entry[(colon + 1)..].Split(Separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var letters = Letters(word).Count;
        return letters == lessons.Length
            ? new ParsedMnemonic(word, lessons, null)
            : new ParsedMnemonic(word, lessons, $"'{word}' has {letters} letters but {lessons.Length} lessons.");
    }

    public static IReadOnlyList<char> Letters(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        var words = word.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 1
            ? [.. words.Select(w => w.FirstOrDefault(char.IsLetter)).Where(char.IsLetter)]
            : [.. word.Where(char.IsLetter)];
    }
}
