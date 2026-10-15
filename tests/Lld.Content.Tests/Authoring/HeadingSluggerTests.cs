using System.Text.Json;
using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

/// <summary>Checks the C# port against vectors produced by the real github-slugger (web/scripts/generate-slug-fixture.mjs).</summary>
public sealed class HeadingSluggerTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static TheoryData<string> CaseNames() => [.. LoadCases().Select(c => c.Name)];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void MatchesGithubSlugger(string caseName)
    {
        var fixture = LoadCases().Single(c => c.Name == caseName);
        var slugger = new HeadingSlugger();

        var slugs = fixture.Headings.Select(slugger.Slug).ToList();

        slugs.Should().Equal(fixture.Slugs);
    }

    [Fact]
    public void Normalize_IgnoresEarlierHeadings()
    {
        HeadingSlugger.Normalize("Overview").Should().Be("overview");
        HeadingSlugger.Normalize("Overview").Should().Be("overview");
    }

    private static IReadOnlyList<SlugCase> LoadCases()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "slugs.json"));
        return JsonSerializer.Deserialize<SlugFixture>(json, Json)!.Cases;
    }

    private sealed record SlugFixture(IReadOnlyList<SlugCase> Cases);

    private sealed record SlugCase(string Name, IReadOnlyList<string> Headings, IReadOnlyList<string> Slugs);
}
