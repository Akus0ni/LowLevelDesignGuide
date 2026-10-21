using FluentAssertions;
using Lld.Content;
using Lld.Contracts;

namespace Lld.Search.Tests;

/// <summary>What a test needs from one way of holding the search tables (device database, API dev index).</summary>
public interface ISearchFixture : IAsyncDisposable
{
    ISearchIndex Index { get; }

    /// <summary>Loads (or reloads) content the way that side does.</summary>
    Task LoadContentAsync(ContentPack pack);

    /// <summary>Writes a live note, or deletes it when <paramref name="deleted"/> (soft delete, spec 007).</summary>
    Task SaveNoteAsync(string ownerKey, Guid id, string lessonId, string anchor, string body, bool deleted = false);
}

/// <summary>
/// The behaviour every <see cref="ISearchIndex"/> must have (spec A1, A2, A3, A7), run against each implementation by a
/// subclass, like the progress and notes store contracts.
/// </summary>
public abstract class SearchIndexContract
{
    protected const string Alice = "user:alice";
    protected const string Bob = "user:bob";

    protected abstract Task<ISearchFixture> CreateAsync();

    [Fact]
    public async Task FindsALessonSectionByAWordInItsText()
    {
        var results = await SearchAsync("billing ");

        var hit = results.Hits.Should().ContainSingle().Subject;
        hit.Should().BeEquivalentTo(new
        {
            Kind = SearchKinds.Lesson, ModuleSlug = SearchTestPack.Solid, LessonId = SearchTestPack.Srp, LessonSlug = "srp",
            LessonTitle = "Single Responsibility", Anchor = "the-idea", Heading = "The idea", NoteId = (string?)null,
        });
        Marked(hit.Snippet).Should().Equal("billing");
    }

    [Fact]
    public async Task MatchesAHalfTypedWord()
    {
        var results = await SearchAsync("bufferedstr");

        results.Hits.Should().ContainSingle().Which.Anchor.Should().Be("seen-in-net");
    }

    [Fact]
    public async Task MatchesOtherFormsOfAWord()
    {
        var results = await SearchAsync("inheritance ");

        results.Hits.Select(h => (h.LessonId, h.Anchor)).Should().BeEquivalentTo([(SearchTestPack.Srp, "the-idea"), (SearchTestPack.Decorator, "the-idea")]);
        results.Hits.Should().ContainSingle(h => h.LessonId == SearchTestPack.Decorator).Which.Snippet.Text.Should().Contain("inherits");
    }

    [Fact]
    public async Task NeedsEveryWord()
    {
        (await SearchAsync("wraps stream ")).Hits.Should().ContainSingle().Which.Anchor.Should().Be("seen-in-net");
    }

    [Fact]
    public async Task MatchesAPhraseOnlyWhenTheWordsAreNextToEachOther()
    {
        (await SearchAsync("\"open closed\"")).Hits.Should().ContainSingle().Which.LessonId.Should().Be(SearchTestPack.Observer);
        (await SearchAsync("\"closed open\"")).Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task ExpandsSynonyms()
    {
        var results = await SearchAsync("srp ");

        results.Expansions.Should().Equal("single responsibility");
        results.Hits.Should().Contain(h => h.LessonId == SearchTestPack.Srp);
    }

    [Theory]
    [InlineData("\"")]
    [InlineData("AND (")]
    [InlineData("NEAR(a b")]
    [InlineData("title: decorator *")]
    [InlineData("^-*")]
    [InlineData("💥 wraps")]
    public async Task NeverFailsOnAnyInput(string input)
    {
        var search = () => SearchAsync(input);

        await search.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ATitleMatchComesFirstAndShowsOncePerLessonAndKind()
    {
        var results = await SearchAsync("decorator ");

        results.Hits.Select(h => (h.Kind, h.LessonId)).Should().Equal(
            (SearchKinds.Lesson, SearchTestPack.Decorator), (SearchKinds.CheatSheet, SearchTestPack.Decorator));
        results.Hits[0].TitleMatch.Should().BeTrue();
        results.Hits[0].Anchor.Should().BeEmpty("the lesson's intro stands for a title-only match");
    }

    [Fact]
    public async Task ShowsTheHookWhenTheIntroHasNoText()
    {
        var results = await SearchAsync("observer ");

        results.Hits[0].Snippet.Text.Should().Be("Tell me when it changes");
    }

    [Fact]
    public async Task FiltersByKindAndCountsEveryKind()
    {
        var all = await SearchAsync("wraps ");
        var sheets = await SearchAsync("wraps ", kind: SearchKinds.CheatSheet);

        sheets.Hits.Should().OnlyContain(h => h.Kind == SearchKinds.CheatSheet).And.ContainSingle();
        sheets.Total.Should().Be(1);
        sheets.CountsByKind.Should().BeEquivalentTo(all.CountsByKind);
        all.CountsByKind[SearchKinds.Lesson].Should().Be(3, "\"wrapped\" in the idea section stems to \"wrap\" too");
    }

    [Fact]
    public async Task FiltersByModule()
    {
        var results = await SearchAsync("class ", module: SearchTestPack.Patterns);

        results.Hits.Should().BeEmpty();
        (await SearchAsync("class ")).Hits.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OffersSuggestionsWhenTheQueryIsTooShort()
    {
        var results = await SearchAsync("a");

        results.Hits.Should().BeEmpty();
        results.Suggestions.Should().Equal("SOLID", "Observer");
    }

    [Fact]
    public async Task ReturnsTheMatchedWordsOfALessonForTheReader()
    {
        var results = await SearchAsync("wrap", lessonId: SearchTestPack.Decorator);

        results.Hits.Should().OnlyContain(h => h.LessonId == SearchTestPack.Decorator);
        results.Words.Should().Contain(["Wraps", "wrapper"]);
    }

    [Fact]
    public async Task FindsTheOwnersNotesOnly()
    {
        await using var fixture = await LoadedAsync();
        await fixture.SaveNoteAsync(Alice, Guid.NewGuid(), SearchTestPack.Decorator, "the-idea", "Used this for #retry logging");

        var alice = await fixture.Index.SearchAsync(Alice, new SearchRequestDto("retry"), TestContext.Current.CancellationToken);
        var bob = await fixture.Index.SearchAsync(Bob, new SearchRequestDto("retry"), TestContext.Current.CancellationToken);

        alice.Hits.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Kind = SearchKinds.Note, ModuleSlug = SearchTestPack.Patterns, LessonTitle = "Decorator", Anchor = "the-idea", Heading = "The idea",
        });
        bob.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task ShowsANoteEditAndDeleteAtOnce()
    {
        await using var fixture = await LoadedAsync();
        var id = Guid.NewGuid();
        await fixture.SaveNoteAsync(Alice, id, SearchTestPack.Srp, "", "first draft");
        await fixture.SaveNoteAsync(Alice, id, SearchTestPack.Srp, "", "rewritten idea");

        (await NoteHitsAsync(fixture, "draft")).Should().BeEmpty();
        (await NoteHitsAsync(fixture, "rewritten")).Should().ContainSingle().Which.NoteId.Should().Be(id.ToString());

        await fixture.SaveNoteAsync(Alice, id, SearchTestPack.Srp, "", "rewritten idea", deleted: true);

        (await NoteHitsAsync(fixture, "rewritten")).Should().BeEmpty();
    }

    [Fact]
    public async Task IndexesANoteOnARemovedSectionUnderTheWholeLesson()
    {
        await using var fixture = await LoadedAsync();
        await fixture.SaveNoteAsync(Alice, Guid.NewGuid(), SearchTestPack.Srp, "gone-section", "orphan words");
        await fixture.SaveNoteAsync(Alice, Guid.NewGuid(), "removed-lesson", "", "lost lesson words");

        (await NoteHitsAsync(fixture, "orphan")).Should().ContainSingle().Which.Heading.Should().Be("Whole lesson");
        (await NoteHitsAsync(fixture, "lost")).Should().ContainSingle().Which.ModuleSlug.Should().BeEmpty();
    }

    [Fact]
    public async Task ReloadingContentKeepsNotesAndRenamesTheirLesson()
    {
        await using var fixture = await LoadedAsync();
        await fixture.SaveNoteAsync(Alice, Guid.NewGuid(), SearchTestPack.Decorator, "", "kept note");

        await fixture.LoadContentAsync(SearchTestPack.Create(decoratorTitle: "Decorator (wrapper)"));

        (await NoteHitsAsync(fixture, "kept")).Should().ContainSingle().Which.LessonTitle.Should().Be("Decorator (wrapper)");
    }

    protected static IEnumerable<string> Marked(SearchSnippetDto snippet) =>
        snippet.Marks.Select(mark => snippet.Text.Substring(mark[0], mark[1]));

    private async Task<ISearchFixture> LoadedAsync()
    {
        var fixture = await CreateAsync();
        await fixture.LoadContentAsync(SearchTestPack.Create());
        return fixture;
    }

    private async Task<SearchResultsDto> SearchAsync(string query, string? kind = null, string? module = null, string? lessonId = null)
    {
        await using var fixture = await LoadedAsync();
        return await fixture.Index.SearchAsync(Alice, new SearchRequestDto(query, kind, module, lessonId), TestContext.Current.CancellationToken);
    }

    private static async Task<IReadOnlyList<SearchHitDto>> NoteHitsAsync(ISearchFixture fixture, string query) =>
        (await fixture.Index.SearchAsync(Alice, new SearchRequestDto(query, SearchKinds.Note), default)).Hits;
}
