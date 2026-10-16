using FluentAssertions;
using Lld.Contracts;

namespace Lld.Content.Tests;

/// <summary>
/// Behaviour every <see cref="IContentCatalog"/> must have. Each implementation (in memory, SQLite) derives from this
/// class, so the dev API and the device can never disagree.
/// </summary>
public abstract class ContentCatalogContract
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected abstract Task<IContentCatalog> CreateCatalogAsync(ContentPack pack);

    private Task<IContentCatalog> SampleAsync() => CreateCatalogAsync(TestPacks.Sample());

    [Fact]
    public async Task Modules_AreInDisplayOrder_WithLessonCounts()
    {
        var catalog = await SampleAsync();

        var modules = await catalog.GetModulesAsync(Ct);

        modules.Should().Equal(
            new ModuleSummary("start", "Start here", "Start here summary.", 0, 1),
            new ModuleSummary("oop-solid", "OOP & SOLID", "OOP & SOLID summary.", 1, 2),
            new ModuleSummary("case-studies", "Case studies", "Case studies summary.", 2, 3));
    }

    [Fact]
    public async Task Module_ListsItsLessonsInOrder()
    {
        var catalog = await SampleAsync();

        var module = await catalog.GetModuleAsync("case-studies", Ct);

        module.Should().NotBeNull();
        module!.Title.Should().Be("Case studies");
        module.Lessons.Select(l => l.Id).Should().Equal("case-study-auth", "case-study-parking", "case-study-lru");
        module.Lessons[0].Should().BeEquivalentTo(new LessonSummary(
            "case-study-auth", "case-studies", "auth", "Authentication", "Authentication in one sentence.", 1, 3, ["tag-b", "tag-a"]),
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Module_CarriesItsMap_OrNullWithoutOne()
    {
        var catalog = await SampleAsync();

        (await catalog.GetModuleAsync("oop-solid", Ct))!.Map.Should().BeEquivalentTo(TestPacks.SampleMap, o => o.WithStrictOrdering());
        (await catalog.GetModuleAsync("case-studies", Ct))!.Map.Should().BeNull();
    }

    [Fact]
    public async Task MemoryAids_ReachEveryView()
    {
        var catalog = await SampleAsync();

        var module = await catalog.GetModuleAsync("oop-solid", Ct);
        var lesson = await catalog.GetLessonAsync("solid-srp", Ct);
        var sheets = await catalog.GetCheatSheetsAsync("oop-solid", Ct);
        var sheet = await catalog.GetCheatSheetAsync("solid-srp", Ct);

        module!.Mnemonics.Should().BeEquivalentTo(TestPacks.SampleMnemonics, o => o.WithStrictOrdering());
        module.Lessons.Select(l => (l.Icon, l.Hook)).Should().Equal(("🎯", "One reason to change"), (null, null));
        lesson!.Icon.Should().Be("🎯");
        sheets.Select(s => s.Icon).Should().Equal("🎯", null);
        sheet!.Icon.Should().Be("🎯");
        (await catalog.GetModuleAsync("case-studies", Ct))!.Mnemonics.Should().BeEmpty();
    }

    [Fact]
    public async Task Module_Unknown_IsNull()
    {
        var catalog = await SampleAsync();

        (await catalog.GetModuleAsync("nope", Ct)).Should().BeNull();
    }

    [Fact]
    public async Task Lesson_CarriesBodyOutlineModuleAndCheatSheetTitle()
    {
        var catalog = await SampleAsync();

        var lesson = await catalog.GetLessonAsync("solid-srp", Ct);

        lesson.Should().NotBeNull();
        lesson!.ModuleSlug.Should().Be("oop-solid");
        lesson.ModuleTitle.Should().Be("OOP & SOLID");
        lesson.Slug.Should().Be("srp");
        lesson.BodyMarkdown.Should().Be("# Single responsibility\n\n## Overview\n\nBody of solid-srp.");
        lesson.Outline.Should().Equal(new OutlineEntry(2, "Overview", "overview"));
        lesson.CheatSheetTitle.Should().Be("Cheat sheet: Single responsibility");
        lesson.Tags.Should().Equal("tag-b", "tag-a");
        lesson.Terms.Should().Equal(new LessonTerm("Token", "A Single responsibility token.", "overview"));
    }

    [Fact]
    public async Task Lesson_PreviousAndNext_FollowModuleOrder_AndStayInsideTheModule()
    {
        var catalog = await SampleAsync();

        var first = await catalog.GetLessonAsync("case-study-auth", Ct);
        var middle = await catalog.GetLessonAsync("case-study-parking", Ct);
        var last = await catalog.GetLessonAsync("case-study-lru", Ct);
        var only = await catalog.GetLessonAsync("how-to-use", Ct);

        first!.Previous.Should().BeNull();
        first.Next.Should().Be(new LessonLink("case-study-parking", "case-studies", "parking-lot", "Parking lot"));
        middle!.Previous.Should().Be(new LessonLink("case-study-auth", "case-studies", "auth", "Authentication"));
        middle.Next.Should().Be(new LessonLink("case-study-lru", "case-studies", "lru-cache", "LRU cache"));
        last!.Next.Should().BeNull();
        only!.Previous.Should().BeNull();
        only.Next.Should().BeNull();
    }

    [Fact]
    public async Task Lesson_Unknown_IsNull()
    {
        var catalog = await SampleAsync();

        (await catalog.GetLessonAsync("nope", Ct)).Should().BeNull();
    }

    [Fact]
    public async Task CheatSheets_All_InModuleThenLessonOrder()
    {
        var catalog = await SampleAsync();

        var sheets = await catalog.GetCheatSheetsAsync(null, Ct);

        sheets.Select(s => s.LessonId).Should().Equal(
            "how-to-use", "solid-srp", "solid-ocp", "case-study-auth", "case-study-parking", "case-study-lru");
        sheets[1].Should().Be(new CheatSheetSummary(
            "solid-srp", "oop-solid", "OOP & SOLID", "srp", "Single responsibility", "Cheat sheet: Single responsibility", "🎯"));
    }

    [Fact]
    public async Task CheatSheets_FilteredByModule()
    {
        var catalog = await SampleAsync();

        var sheets = await catalog.GetCheatSheetsAsync("oop-solid", Ct);

        sheets.Select(s => s.LessonId).Should().Equal("solid-srp", "solid-ocp");
    }

    [Fact]
    public async Task CheatSheets_UnknownModule_IsEmpty()
    {
        var catalog = await SampleAsync();

        (await catalog.GetCheatSheetsAsync("nope", Ct)).Should().BeEmpty();
    }

    [Fact]
    public async Task CheatSheet_CarriesBodyAndItsLesson()
    {
        var catalog = await SampleAsync();

        var sheet = await catalog.GetCheatSheetAsync("case-study-auth", Ct);

        sheet.Should().Be(new CheatSheetDetail(
            "case-study-auth",
            "case-studies",
            "Case studies",
            "auth",
            "Authentication",
            "Cheat sheet: Authentication",
            "# Cheat sheet: Authentication\n\n- point"));
    }

    [Fact]
    public async Task CheatSheet_Unknown_IsNull()
    {
        var catalog = await SampleAsync();

        (await catalog.GetCheatSheetAsync("nope", Ct)).Should().BeNull();
    }
}
