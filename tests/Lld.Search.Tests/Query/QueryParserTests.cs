using FluentAssertions;
using Lld.Search.Query;

namespace Lld.Search.Tests.Query;

public sealed class QueryParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NothingTyped_IsEmpty(string? input) =>
        QueryParser.Parse(input).IsEmpty.Should().BeTrue();

    [Fact]
    public void Parse_LongInput_IsCutAtTheLimit()
    {
        var input = new string('x', QueryParser.MaxLength) + " overflow";

        var query = QueryParser.Parse(input);

        query.Terms.Should().ContainSingle().Which.Words.Single().Should().HaveLength(QueryParser.MaxLength);
    }

    /// <summary>Spec A3: nothing typed can produce an expression FTS5 would reject (checked against SQLite in the index tests).</summary>
    [Theory]
    [InlineData("\"")]
    [InlineData("\"\"\"")]
    [InlineData("a\"b\"c")]
    [InlineData("NOT")]
    [InlineData("OR OR OR")]
    [InlineData("col:value")]
    [InlineData("^start")]
    [InlineData("{a b}")]
    [InlineData("a + b")]
    [InlineData("💥💥💥")]
    [InlineData("\u0000\u001f")]
    public void Parse_HostileInput_OnlyYieldsQuotedWords(string input)
    {
        var expanded = SynonymExpander.None.Expand(QueryParser.Parse(input));

        foreach (var expression in new[] { MatchExpressionBuilder.Stem(expanded), MatchExpressionBuilder.Rest(expanded), MatchExpressionBuilder.Prefix(expanded) })
        {
            expression.Replace(" AND ", " ", StringComparison.Ordinal).Replace(" OR ", " ", StringComparison.Ordinal)
                .Where(c => !char.IsLetterOrDigit(c) && c is not ('"' or ' ' or '*' or '(' or ')'))
                .Should().BeEmpty();
        }
    }
}
