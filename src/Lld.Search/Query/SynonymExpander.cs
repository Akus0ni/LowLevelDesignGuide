using System.Globalization;

namespace Lld.Search.Query;

/// <summary>A query term and the alternatives that may match in its place (<c>srp</c> → "single responsibility").</summary>
public sealed record ExpandedTerm(QueryTerm Term, IReadOnlyList<QueryTerm> Alternatives);

/// <param name="Expansions">The alternatives used, as text, for "Also searching: …" (spec §3.2).</param>
public sealed record ExpandedQuery(IReadOnlyList<ExpandedTerm> Terms, bool LastIsPrefix, IReadOnlyList<string> Expansions)
{
    public bool IsEmpty => Terms.Count == 0;
}

/// <summary>
/// Expands acronyms and synonyms from <c>content/search.yml</c> (spec §3.3). One-way: a single-word term equal to a
/// key also matches the key's alternatives; an alternative never expands back to its key.
/// </summary>
public sealed class SynonymExpander
{
    private readonly Dictionary<string, IReadOnlyList<QueryTerm>> _alternatives;

    public SynonymExpander(IReadOnlyDictionary<string, IReadOnlyList<string>> synonyms)
    {
        ArgumentNullException.ThrowIfNull(synonyms);
        _alternatives = new Dictionary<string, IReadOnlyList<QueryTerm>>(StringComparer.Ordinal);
        foreach (var (key, alternatives) in synonyms)
        {
            var parsed = alternatives.Select(Single).OfType<QueryTerm>().ToList();
            if (parsed.Count > 0)
            {
                _alternatives[key.ToLower(CultureInfo.InvariantCulture)] = parsed;
            }
        }
    }

    public static SynonymExpander None { get; } = new(new Dictionary<string, IReadOnlyList<string>>());

    public ExpandedQuery Expand(SearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.IsEmpty)
        {
            return new ExpandedQuery([], LastIsPrefix: false, []);
        }

        var expansions = new List<string>();
        var terms = query.Terms.Select(term =>
        {
            var alternatives = !term.IsPhrase && _alternatives.TryGetValue(term.Words[0], out var found) ? found : [];
            expansions.AddRange(alternatives.Select(alternative => string.Join(' ', alternative.Words)));
            return new ExpandedTerm(term, alternatives);
        }).ToList();
        return new ExpandedQuery(terms, query.LastIsPrefix, [.. expansions.Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>An alternative is parsed like a query, as one word or one phrase.</summary>
    private static QueryTerm? Single(string alternative)
    {
        var words = QueryParser.Parse(alternative).Terms.SelectMany(term => term.Words).ToList();
        return words.Count switch
        {
            0 => null,
            1 => QueryTerm.Word(words[0]),
            _ => QueryTerm.Phrase(words),
        };
    }
}
