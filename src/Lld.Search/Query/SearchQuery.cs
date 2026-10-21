namespace Lld.Search.Query;

/// <summary>One thing the learner typed: a single word, or the words of a <c>"quoted phrase"</c>, lower-cased.</summary>
public sealed record QueryTerm(IReadOnlyList<string> Words, bool IsPhrase)
{
    public static QueryTerm Word(string word) => new([word], IsPhrase: false);

    public static QueryTerm Phrase(IReadOnlyList<string> words) => new(words, IsPhrase: true);

    public int Length => Words.Sum(word => word.Length);

    public override string ToString() => IsPhrase ? $"\"{string.Join(' ', Words)}\"" : Words[0];
}

/// <summary>
/// A parsed query: plain words and phrases, never FTS5 syntax. All terms must match (AND).
/// </summary>
/// <param name="LastIsPrefix">The learner is still typing the last word, so it matches as a prefix too.</param>
public sealed record SearchQuery(IReadOnlyList<QueryTerm> Terms, bool LastIsPrefix)
{
    /// <summary>Searching starts at this many letters or digits (spec §3.2).</summary>
    public const int MinLength = 2;

    public static SearchQuery Empty { get; } = new([], LastIsPrefix: false);

    /// <summary>Too short to search: the UI shows recent searches instead.</summary>
    public bool IsEmpty => Terms.Sum(term => term.Length) < MinLength;

    public override string ToString() => string.Join(' ', Terms) + (LastIsPrefix ? "*" : string.Empty);
}
