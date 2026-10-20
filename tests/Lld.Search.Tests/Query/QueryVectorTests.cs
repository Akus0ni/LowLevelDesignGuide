using System.Text.Json;
using FluentAssertions;
using Lld.Search.Query;

namespace Lld.Search.Tests.Query;

/// <summary>
/// Parse → expand → MATCH, case by case from <c>tests/fixtures/search/queries.json</c> (spec A2, A3). Each case pins
/// what the learner typed to the exact FTS5 expressions, so a change in quoting or prefix handling shows up here.
/// </summary>
public sealed class QueryVectorTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<Vector> Loaded = new(Load);

    public static TheoryData<string> CaseNames => [.. Loaded.Value.Cases.Select(c => c.Name)];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Query_MatchesTheVector(string name)
    {
        var vector = Loaded.Value;
        var queryCase = vector.Cases.Single(c => c.Name == name);

        var query = QueryParser.Parse(queryCase.Input);
        var expanded = new SynonymExpander(vector.Synonyms).Expand(query);

        query.ToString().Should().Be(queryCase.Query);
        query.IsEmpty.Should().Be(queryCase.Empty);
        MatchExpressionBuilder.Stem(expanded).Should().Be(queryCase.Stem);
        MatchExpressionBuilder.Rest(expanded).Should().Be(queryCase.Rest);
        MatchExpressionBuilder.Prefix(expanded).Should().Be(queryCase.Prefix);
        expanded.Expansions.Should().Equal(queryCase.Expansions ?? []);
    }

    private static Vector Load()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "search", "queries.json"));
        return JsonSerializer.Deserialize<Vector>(json, Json)!;
    }

    private sealed record Vector(IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms, IReadOnlyList<QueryCase> Cases);

    private sealed record QueryCase(
        string Name, string Input, string Query, string Stem, string Rest, string Prefix, IReadOnlyList<string>? Expansions, bool Empty);
}
