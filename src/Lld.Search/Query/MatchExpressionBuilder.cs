namespace Lld.Search.Query;

/// <summary>
/// Builds FTS5 MATCH expressions from an <see cref="ExpandedQuery"/> (plan §2.2). Every word is written as a quoted
/// FTS5 string and only holds letters and digits (<see cref="QueryParser"/>), so the expression can't be malformed.
/// <list type="bullet">
/// <item><see cref="Stem"/>: every term as a whole word, for the stemmed index.</item>
/// <item><see cref="Rest"/>: every term but the last, for the stemmed index (empty when there is one term).</item>
/// <item><see cref="Prefix"/>: the last term as a prefix, for the unstemmed index (empty when it is not a prefix).</item>
/// </list>
/// </summary>
public static class MatchExpressionBuilder
{
    public static string Stem(ExpandedQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return And(query.Terms.Select(term => Term(term, prefix: false)));
    }

    public static string Rest(ExpandedQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return And(query.Terms.SkipLast(1).Select(term => Term(term, prefix: false)));
    }

    public static string Prefix(ExpandedQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.LastIsPrefix && query.Terms.Count > 0 ? Term(query.Terms[^1], prefix: true) : string.Empty;
    }

    private static string And(IEnumerable<string> parts) => string.Join(" AND ", parts);

    private static string Term(ExpandedTerm term, bool prefix)
    {
        var own = Quoted(term.Term) + (prefix ? "*" : string.Empty);
        return term.Alternatives.Count == 0
            ? own
            : $"({string.Join(" OR ", term.Alternatives.Select(Quoted).Prepend(own))})";
    }

    private static string Quoted(QueryTerm term) => $"\"{string.Join(' ', term.Words)}\"";
}
