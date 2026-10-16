using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Tests.Authoring;

/// <summary>Each test breaks one thing in <see cref="ValidRepo"/> and expects exactly one issue (path, line, code).</summary>
public sealed class ValidationTests
{
    [Fact]
    public void ValidRepo_HasNoIssues()
    {
        var result = Build(new ValidRepo());

        result.Issues.Should().BeEmpty();
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ModuleWithoutManifest() =>
        IssuesOf(new ValidRepo().Remove(ValidRepo.StartModule))
            .Should().Equal((ValidRepo.StartModule, null, IssueCodes.ModuleManifestMissing));

    [Fact]
    public void LessonFolderWithoutLesson() =>
        IssuesOf(new ValidRepo().Remove(ValidRepo.Parking))
            .Should().Equal((ValidRepo.Parking, null, IssueCodes.LessonMissing));

    [Fact]
    public void LessonWithoutCheatSheet() =>
        IssuesOf(new ValidRepo().Remove(ValidRepo.ParkingSheet))
            .Should().Equal((ValidRepo.ParkingSheet, null, IssueCodes.CheatSheetMissing));

    [Fact]
    public void LessonWithoutFrontMatter() =>
        IssuesOf(new ValidRepo().Set(ValidRepo.Parking, "# Parking lot\n"))
            .Should().Equal((ValidRepo.Parking, 1, IssueCodes.FrontMatterMissing));

    [Theory]
    [InlineData("title: Parking lot", "title: Parking: lot", 3)]
    [InlineData("tags: []", "tags: [oops", 2)] // YamlDotNet gives no position for this one: the front-matter start is used.
    public void UnreadableFrontMatter_IsReportedOnce(string oldText, string newText, int line) =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, oldText, newText))
            .Should().Equal((ValidRepo.Parking, line, YamlMapParser.InvalidCode));

    [Fact]
    public void LessonMissingARequiredKey_PointsAtTheFrontMatter() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, "summary: Classic.\n", string.Empty))
            .Should().Equal((ValidRepo.Parking, 2, IssueCodes.FrontMatterMissingKey));

    [Theory]
    [InlineData("order: 2", "order: second", 4)]
    [InlineData("order: 2", "order: -1", 4)]
    [InlineData("tags: []", "tags: classic", 6)]
    [InlineData("source: classic", "source: blog", 7)]
    [InlineData("title: Parking lot", "title: \"  \"", 3)]
    public void LessonKeyOfTheWrongType(string oldText, string newText, int line) =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, oldText, newText))
            .Should().Equal((ValidRepo.Parking, line, IssueCodes.FrontMatterWrongType));

    [Fact]
    public void LessonWithAnUnknownKey_SuchAsTheOldModuleKey() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, "order: 2\n", "order: 2\nmodule: case-studies\n"))
            .Should().Equal((ValidRepo.Parking, 5, IssueCodes.FrontMatterUnknownKey));

    [Fact]
    public void CheatSheetWithMoreThanATitle() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.ParkingSheet, "---\n#", "order: 2\n---\n#"))
            .Should().Equal((ValidRepo.ParkingSheet, 3, IssueCodes.FrontMatterUnknownKey));

    [Fact]
    public void ModuleManifestMissingAKey() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.StartModule, "order: 0\n", string.Empty))
            .Should().Equal((ValidRepo.StartModule, 1, IssueCodes.FrontMatterMissingKey));

    [Fact]
    public void DuplicateLessonId_IsReportedOnTheSecondOne() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, "id: case-study-parking", "id: how-to-use"))
            .Should().Equal((ValidRepo.HowToUse, 2, IssueCodes.IdDuplicate));

    [Fact]
    public void LessonIdThatIsNotKebabCase() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, "id: case-study-parking", "id: Case_Study_Parking"))
            .Should().Equal((ValidRepo.Parking, 2, IssueCodes.IdFormat));

    [Fact]
    public void FolderNameThatIsNotKebabCase() =>
        IssuesOf(new ValidRepo()
                .Set("content/case-studies/LRU Cache/lesson.md", "---\nid: case-study-lru\ntitle: LRU\norder: 3\nsummary: S.\ntags: []\n---\n# LRU\n")
                .Set("content/case-studies/LRU Cache/cheatsheet.md", "---\ntitle: LRU\n---\n# LRU\n"))
            .Should().Equal(("content/case-studies/LRU Cache", null, IssueCodes.SlugFormat));

    [Fact]
    public void TwoModulesWithTheSameOrder() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.CaseStudiesModule, "order: 2", "order: 0"))
            .Should().Equal((ValidRepo.StartModule, 2, IssueCodes.OrderDuplicate));

    [Fact]
    public void TwoLessonsInAModuleWithTheSameOrder() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Parking, "order: 2", "order: 1"))
            .Should().Equal((ValidRepo.Parking, 4, IssueCodes.OrderDuplicate));

    [Fact]
    public void LinkToALessonThatDoesNotExist() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Auth, "../../start/how-to-use/lesson.md", "../../start/nope/lesson.md"))
            .Should().Equal((ValidRepo.Auth, 13, IssueCodes.LinkBroken));

    [Fact]
    public void LinkToAHeadingThatDoesNotExist() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.HowToUse, "#token-rotation", "#token-refresh"))
            .Should().Equal((ValidRepo.HowToUse, 12, IssueCodes.LinkAnchorMissing));

    [Fact]
    public void LinkToAHeadingInTheSameFileThatDoesNotExist() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.HowToUse, "(#modules)", "(#lessons)"))
            .Should().Equal((ValidRepo.HowToUse, 13, IssueCodes.LinkAnchorMissing));

    [Fact]
    public void LinkThatLeavesTheRepository() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Auth, "../../start/how-to-use/lesson.md", "../../../../elsewhere.md"))
            .Should().Equal((ValidRepo.Auth, 13, IssueCodes.LinkInvalid));

    [Fact]
    public void LinkToARepositoryFileThatDoesNotExist() =>
        IssuesOf(new ValidRepo().Remove(ValidRepo.SourceFile))
            .Should().Equal((ValidRepo.Auth, 24, IssueCodes.LinkRepositoryFileMissing));

    [Fact]
    public void Image_IsNotSupportedYet() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.ParkingSheet, "# Parking lot\n", "# Parking lot\n\n![lot](lot.png)\n"))
            .Should().Equal((ValidRepo.ParkingSheet, 6, IssueCodes.ImageUnsupported));

    [Fact]
    public void EmptyMermaidBlock() =>
        IssuesOf(new ValidRepo().Replace(ValidRepo.Auth, "stateDiagram-v2\n  Pending --> Approved\n", "\n"))
            .Should().Equal((ValidRepo.Auth, 15, IssueCodes.MermaidEmpty));

    [Fact]
    public void Issues_AreSortedByPathThenLine_AndPrintLikeACompiler()
    {
        var issues = Build(new ValidRepo()
            .Remove(ValidRepo.ParkingSheet)
            .Replace(ValidRepo.Parking, "order: 2", "order: x")
            .Replace(ValidRepo.Parking, "tags: []", "tags: x")).Issues;

        issues.Select(i => i.ToString()).Should().Equal(
            $"{ValidRepo.ParkingSheet}: {IssueCodes.CheatSheetMissing}: Every lesson needs a cheatsheet.md for quick revision.",
            $"{ValidRepo.Parking}:4: {IssueCodes.FrontMatterWrongType}: 'order' must be a whole number (0 or more).",
            $"{ValidRepo.Parking}:6: {IssueCodes.FrontMatterWrongType}: 'tags' must be a list, e.g. [a, b].");
    }

    [Fact]
    public void MissingContentFolder_FailsTheBuild()
    {
        var result = new ContentPipeline(new Lld.Content.Authoring.Files.InMemoryContentFiles(), new ContentOptions()).Build();

        result.Succeeded.Should().BeFalse();
        result.Pack.Should().BeNull();
        result.Issues.Should().ContainSingle().Which.Code.Should().Be("content.missing");
    }

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
