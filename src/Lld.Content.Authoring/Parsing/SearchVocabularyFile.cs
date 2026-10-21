using YamlDotNet.RepresentationModel;

namespace Lld.Content.Authoring.Parsing;

/// <param name="Line">1-based line of the key in the file.</param>
public sealed record ParsedSynonym(string Key, IReadOnlyList<string> Alternatives, int Line);

/// <param name="Line">1-based line of the suggestion in the file.</param>
public sealed record ParsedSuggestion(string Text, int Line);

public sealed record ParsedSearchVocabulary(
    IReadOnlyList<ParsedSynonym> Synonyms, IReadOnlyList<ParsedSuggestion> Suggestions, IReadOnlyList<LineProblem> Problems)
{
    /// <summary>What the pack carries: keys lower-cased (matching ignores case), suggestions as written.</summary>
    public SearchVocabulary ToPack() => new(
        Synonyms
            .GroupBy(s => s.Key.ToLowerInvariant(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)[.. g.First().Alternatives], StringComparer.Ordinal),
        [.. Suggestions.Select(s => s.Text)]);
}

/// <summary>
/// Reads <c>content/search.yml</c> (spec 009 §3.3):
/// <code>
/// synonyms:
///   SRP: [single responsibility]
/// suggestions:
///   - SOLID
/// </code>
/// Only the shape is checked here; the vocabulary rules check keys, alternatives and suggestions against the content.
/// </summary>
public static class SearchVocabularyFile
{
    public const string SynonymsKey = "synonyms";
    public const string SuggestionsKey = "suggestions";

    private static readonly string[] Keys = [SynonymsKey, SuggestionsKey];

    public static ParsedSearchVocabulary Parse(YamlMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var problems = new List<LineProblem>();
        foreach (var unknown in map.Entries.Where(e => !Keys.Contains(e.Key, StringComparer.Ordinal)))
        {
            problems.Add(new(unknown.Line, $"Unknown key '{unknown.Key}'. Allowed: {string.Join(", ", Keys)}."));
        }

        return new ParsedSearchVocabulary(Synonyms(map[SynonymsKey], problems), Suggestions(map[SuggestionsKey], problems), problems);
    }

    private static List<ParsedSynonym> Synonyms(YamlEntry? entry, List<LineProblem> problems)
    {
        if (entry is null)
        {
            return [];
        }

        if (entry.Nested is not { Root: YamlMappingNode map } nested)
        {
            problems.Add(new(entry.Line, "Write synonyms as 'WORD: [alternative, other alternative]' lines."));
            return [];
        }

        var synonyms = new List<ParsedSynonym>();
        foreach (var (keyNode, value) in map.Children)
        {
            var line = nested.LineOf(keyNode);
            var key = (keyNode as YamlScalarNode)?.Value?.Trim() ?? string.Empty;
            IReadOnlyList<string> alternatives = value switch
            {
                YamlSequenceNode list when list.Children.All(i => i is YamlScalarNode) =>
                    [.. list.Children.Cast<YamlScalarNode>().Select(i => i.Value?.Trim() ?? string.Empty)],
                YamlScalarNode { Value: { } single } => [single.Trim()],
                _ => [],
            };
            if (key.Length == 0 || alternatives.Count == 0 || alternatives.Any(a => a.Length == 0))
            {
                problems.Add(new(line, $"Synonym '{key}' needs a word and a list of alternatives, e.g. 'SRP: [single responsibility]'."));
                continue;
            }

            synonyms.Add(new ParsedSynonym(key, alternatives, line));
        }

        return synonyms;
    }

    private static List<ParsedSuggestion> Suggestions(YamlEntry? entry, List<LineProblem> problems)
    {
        if (entry is null)
        {
            return [];
        }

        if (entry.Kind != YamlValueKind.List)
        {
            problems.Add(new(entry.Line, "Write suggestions as a list of searches: '- SOLID'."));
            return [];
        }

        // A flat list loses its item positions in YamlMap, so suggestions are reported at the key's line plus their index.
        return [.. entry.Items.Select((text, i) => new ParsedSuggestion(text.Trim(), entry.Line + 1 + i)).Where(s => s.Text.Length > 0)];
    }
}
