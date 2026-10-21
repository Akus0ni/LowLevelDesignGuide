using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Lld.Content;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Files;
using Lld.Contracts;
using Lld.Search.Tests.Sqlite;

namespace Lld.Search.Tests;

/// <summary>
/// Search over the real <c>content/</c> folder (spec 009 T5, A10): every suggestion in <c>search.yml</c> finds something,
/// a few known queries land on the right lesson, and the benchmark query set stays fast.
/// </summary>
public sealed class RealContentSearchTests
{
    private const string SolutionFile = "LowLevelDesignGuide.slnx";
    private const string Owner = "guest";

    /// <summary>
    /// Generous on purpose: CI machines are slow and shared. The real target (under 200 ms over the bridge on a phone)
    /// is measured on device in the manual run.
    /// </summary>
    private static readonly TimeSpan MaxP95 = TimeSpan.FromMilliseconds(100);

    private static readonly Lazy<ContentPack> Pack = new(() =>
        new ContentPipeline(new PhysicalContentFiles(RepositoryRoot()), new ContentOptions()).Build().Pack
        ?? throw new InvalidOperationException("content/ does not pack; see RepositoryContentTests."));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static TheoryData<string> Suggestions => [.. Pack.Value.Vocabulary!.Suggestions];

    [Theory]
    [MemberData(nameof(Suggestions))]
    public async Task EverySuggestionFindsSomething(string suggestion)
    {
        await using var fixture = await LoadedAsync();

        var results = await fixture.Index.SearchAsync(Owner, new SearchRequestDto(suggestion), Ct);

        results.Hits.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("decorat", "decorator")]
    [InlineData("SRP", "srp")]
    [InlineData("observer ", "observer")]
    [InlineData("liskov", "lsp")]
    [InlineData("multiplicity", "multiplicity")]
    public async Task KnownQueriesPutTheirLessonFirst(string query, string lessonSlug)
    {
        await using var fixture = await LoadedAsync();

        var results = await fixture.Index.SearchAsync(Owner, new SearchRequestDto(query), Ct);

        results.Hits.Should().NotBeEmpty();
        results.Hits[0].LessonSlug.Should().Be(lessonSlug);
    }

    [Fact]
    public async Task BenchmarkQueriesAreFast()
    {
        await using var fixture = await LoadedAsync();
        var queries = BenchmarkQueries();
        foreach (var query in queries)
        {
            await fixture.Index.SearchAsync(Owner, new SearchRequestDto(query), Ct);
        }

        var times = new List<TimeSpan>();
        foreach (var query in queries)
        {
            var clock = Stopwatch.StartNew();
            await fixture.Index.SearchAsync(Owner, new SearchRequestDto(query), Ct);
            times.Add(clock.Elapsed);
        }

        var p95 = times.Order().ElementAt((int)Math.Ceiling(times.Count * 0.95) - 1);
        p95.Should().BeLessThan(MaxP95);
    }

    private static async Task<MemorySearchFixture> LoadedAsync()
    {
        var fixture = await MemorySearchFixture.CreateAsync();
        await fixture.LoadContentAsync(Pack.Value);
        return fixture;
    }

    private static IReadOnlyList<string> BenchmarkQueries()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "search", "benchmark-queries.json"));
        using var document = JsonDocument.Parse(json);
        return [.. document.RootElement.GetProperty("queries").EnumerateArray().Select(q => q.GetString()!)];
    }

    private static string RepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException($"{SolutionFile} not found above {AppContext.BaseDirectory}.");
    }
}
