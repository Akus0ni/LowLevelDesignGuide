using System.Text.Json;
using FluentAssertions;
using Lld.Domain.Notes;

namespace Lld.Domain.Tests.Notes;

/// <summary>The shared vector keeps this parser and the web's <c>tags.ts</c> in agreement (spec 007 A6).</summary>
public sealed class TagParserTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static TheoryData<string> CaseNames => [.. LoadCases().Select(tagCase => tagCase.Name)];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void Parse_MatchesTheSharedVector(string name)
    {
        var tagCase = LoadCases().Single(c => c.Name == name);

        TagParser.Parse(tagCase.Body).Should().Equal(tagCase.Tags);
    }

    private static IReadOnlyList<TagCase> LoadCases()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "notes", "tags.json"));
        return JsonSerializer.Deserialize<IReadOnlyList<TagCase>>(json, Json)!;
    }

    private sealed record TagCase(string Name, string Body, IReadOnlyList<string> Tags);
}
