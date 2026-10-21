using System.Text.Json;
using Lld.Contracts;
using Lld.Search.Query;
using Microsoft.Data.Sqlite;

namespace Lld.Search.Sqlite;

/// <summary>Opens a new connection to the database that holds the search tables; the caller disposes it.</summary>
public delegate Task<SqliteConnection> OpenSearchConnection(CancellationToken cancellationToken);

/// <summary>
/// <see cref="ISearchIndex"/> over the <see cref="SearchSchema"/> tables (plan §2.2, §2.5). A search is three short
/// reads on one connection: the synonyms, the matching units (phase 1, both indexes), then snippets and marks for the
/// hits kept (phase 2).
/// </summary>
public sealed class SqliteSearchIndex(OpenSearchConnection open) : ISearchIndex
{
    /// <summary>The most hits one search returns (spec §3.4 lists them; the dialog shows fewer).</summary>
    public const int MaxLimit = 50;

    public async Task<SearchResultsDto> SearchAsync(string ownerKey, SearchRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownerKey);
        ArgumentNullException.ThrowIfNull(request);
        await using var connection = await open(cancellationToken).ConfigureAwait(false);
        var query = QueryParser.Parse(request.Query);
        if (query.IsEmpty)
        {
            return Empty(request.Query, await ReadSuggestionsAsync(connection, cancellationToken).ConfigureAwait(false));
        }

        var expanded = new SynonymExpander(await ReadSynonymsAsync(connection, cancellationToken).ConfigureAwait(false)).Expand(query);
        var candidates = await ReadCandidatesAsync(connection, ownerKey, expanded, request, cancellationToken).ConfigureAwait(false);
        var ranked = HitRanking.Rank(candidates, request.Kind, Math.Clamp(request.Limit, 0, MaxLimit));
        var withText = request.LessonId is not null;
        var marks = await ReadMarksAsync(connection, expanded, ranked.Hits, withText, cancellationToken).ConfigureAwait(false);
        var hits = ranked.Hits.Select(hit => ToDto(hit, marks.GetValueOrDefault(hit.Id))).ToList();
        IReadOnlyList<string> words = withText
            ? [.. marks.Values.SelectMany(m => m.Words).Distinct(StringComparer.OrdinalIgnoreCase)]
            : [];
        return new SearchResultsDto(request.Query, expanded.Expansions, ranked.Total, ranked.CountsByKind, hits, words, []);
    }

    private static SearchResultsDto Empty(string query, IReadOnlyList<string> suggestions) =>
        new(query, [], 0, SearchKinds.All.ToDictionary(k => k, _ => 0, StringComparer.Ordinal), [], [], suggestions);

    private static async Task<List<SearchCandidate>> ReadCandidatesAsync(
        SqliteConnection connection, string ownerKey, ExpandedQuery query, SearchRequestDto request, CancellationToken cancellationToken)
    {
        var prefix = MatchExpressionBuilder.Prefix(query);
        var rest = MatchExpressionBuilder.Rest(query);
        var anyStem = MatchExpressionBuilder.Any(query, prefixLast: false);
        var anyRaw = MatchExpressionBuilder.Any(query, prefixLast: true);
        await using var command = SearchContentWriter.Command(connection, null, SearchSql.Candidates(prefix.Length > 0, rest.Length > 0));
        command.Parameters.AddWithValue("$stem", MatchExpressionBuilder.Stem(query));
        command.Parameters.AddWithValue("$titleStem", MatchExpressionBuilder.InColumns(anyStem, "Title"));
        command.Parameters.AddWithValue("$titleAllStem", MatchExpressionBuilder.InColumns(MatchExpressionBuilder.Stem(query), "Title"));
        command.Parameters.AddWithValue("$bodyStem", MatchExpressionBuilder.InColumns(anyStem, "Heading", "Text"));
        if (prefix.Length > 0)
        {
            command.Parameters.AddWithValue("$prefix", prefix);
            command.Parameters.AddWithValue("$titleRaw", MatchExpressionBuilder.InColumns(anyRaw, "Title"));
            command.Parameters.AddWithValue("$titleAllRaw", MatchExpressionBuilder.InColumns(MatchExpressionBuilder.Typed(query), "Title"));
            command.Parameters.AddWithValue("$bodyRaw", MatchExpressionBuilder.InColumns(anyRaw, "Heading", "Text"));
        }

        if (rest.Length > 0 && prefix.Length > 0)
        {
            command.Parameters.AddWithValue("$rest", rest);
        }

        command.Parameters.AddWithValue("$owner", ownerKey);
        command.Parameters.AddWithValue("$module", (object?)request.Module ?? DBNull.Value);
        command.Parameters.AddWithValue("$lesson", (object?)request.LessonId ?? DBNull.Value);
        var candidates = new List<SearchCandidate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            candidates.Add(new SearchCandidate(
                reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetString(9),
                reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetDouble(12),
                reader.GetBoolean(13), reader.GetBoolean(14), reader.GetBoolean(15)));
        }

        return candidates;
    }

    /// <summary>
    /// Snippets from both indexes; per hit the one with more marks wins (the stemmed one marks <c>inheritance</c> for
    /// <c>inherits</c>, the unstemmed one marks a half-typed word).
    /// </summary>
    private static async Task<Dictionary<long, HitMarks>> ReadMarksAsync(
        SqliteConnection connection, ExpandedQuery query, IReadOnlyList<SearchCandidate> hits, bool withText, CancellationToken cancellationToken)
    {
        var marks = new Dictionary<long, HitMarks>();
        if (hits.Count == 0)
        {
            return marks;
        }

        var ids = JsonSerializer.Serialize(hits.Select(h => h.Id).ToList(), SearchJsonContext.Default.ListInt64);
        foreach (var (table, any) in new[] { ("SearchStem", MatchExpressionBuilder.Any(query, prefixLast: false)), ("SearchRaw", MatchExpressionBuilder.Any(query, prefixLast: true)) })
        {
            await using var command = SearchContentWriter.Command(connection, null, SearchSql.Marks(table, withText));
            command.Parameters.AddWithValue("$open", Highlighted.Open);
            command.Parameters.AddWithValue("$close", Highlighted.Close);
            command.Parameters.AddWithValue("$any", any);
            command.Parameters.AddWithValue("$ids", ids);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var found = HitMarks.From(reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
                var id = reader.GetInt64(0);
                marks[id] = marks.TryGetValue(id, out var before) ? before.Best(found) : found;
            }
        }

        return marks;
    }

    private static SearchHitDto ToDto(SearchCandidate hit, HitMarks? marks) =>
        new(hit.Kind, hit.ModuleSlug, hit.LessonId, hit.LessonSlug, hit.LessonTitle, hit.Anchor, hit.Section, hit.NoteId,
            marks?.Snippet ?? new SearchSnippetDto(string.Empty, []), hit.TitleMatch);

    private static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadSynonymsAsync(
        SqliteConnection connection, CancellationToken cancellationToken)
    {
        var synonyms = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        await using var command = SearchContentWriter.Command(connection, null, "SELECT Word, AlternativesJson FROM SearchSynonyms;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            synonyms[reader.GetString(0)] = JsonSerializer.Deserialize(reader.GetString(1), SearchJsonContext.Default.IReadOnlyListString) ?? [];
        }

        return synonyms;
    }

    private static async Task<IReadOnlyList<string>> ReadSuggestionsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var suggestions = new List<string>();
        await using var command = SearchContentWriter.Command(connection, null, "SELECT Text FROM SearchSuggestions ORDER BY Position;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            suggestions.Add(reader.GetString(0));
        }

        return suggestions;
    }

    /// <summary>What one index marked in a hit.</summary>
    private sealed record HitMarks(SearchSnippetDto Snippet, IReadOnlyList<string> Words)
    {
        public static HitMarks From(string snippet, string heading, string? text)
        {
            var parsed = Highlighted.Snippet(snippet);
            if (string.IsNullOrWhiteSpace(parsed.Text))
            {
                // A lesson intro with no text before its first section: show the hook (its heading column) instead.
                parsed = Highlighted.Snippet(heading);
            }

            return new HitMarks(parsed, [.. Highlighted.Words(heading), .. text is null ? [] : Highlighted.Words(text)]);
        }

        public HitMarks Best(HitMarks other) =>
            new(other.Snippet.Marks.Count > Snippet.Marks.Count ? other.Snippet : Snippet, [.. Words, .. other.Words]);
    }
}
