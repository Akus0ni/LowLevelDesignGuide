using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// Spec 006 §3.2, §3.5, §3.8: the <c>pattern</c> template. Each test breaks one thing in
/// <see cref="ValidRepo.WithPatternModule"/> and expects exactly one issue (path, line, code).
/// </summary>
public sealed class PatternContentTests
{
    private const int FirstLine = 1;
    private const int FrontMatterLine = 2;

    [Fact]
    public void PatternRepo_HasNoIssues() => IssuesOf(Patterns()).Should().BeEmpty();

    [Theory]
    [InlineData("## The idea\n\nPass the algorithm in.\n\n")]
    [InlineData("## Seen in .NET\n\n- `IComparer<T>`\n\n")]
    [InlineData("## Travels with\n\n- Factory Method picks one.\n\n")]
    public void MissingSection(string section) =>
        IssuesOf(Patterns().Replace(ValidRepo.Strategy, section, string.Empty))
            .Should().Equal((ValidRepo.Strategy, FirstLine, IssueCodes.PatternSectionMissing));

    [Fact]
    public void SectionOutOfOrder()
    {
        var repo = Patterns().Replace(ValidRepo.Strategy, "## The idea\n", "## In this app\n\nHere.\n\n## The idea\n");

        IssuesOf(repo).Should().Equal((ValidRepo.Strategy, repo.LineOf(ValidRepo.Strategy, "## The idea"), IssueCodes.PatternSectionOrder));
    }

    [Fact]
    public void OptionalSections_InOrder_AreFine() =>
        IssuesOf(Patterns().Replace(ValidRepo.Strategy, "## Interview questions", "## In this app\n\nHere.\n\n## Interview questions"))
            .Should().BeEmpty();

    [Fact]
    public void StructureWithoutClassDiagram()
    {
        var repo = Patterns().Replace(ValidRepo.Strategy, "classDiagram\n  Context --> IStrategy", "flowchart TD\n  a --> b");

        IssuesOf(repo).Should().Equal((ValidRepo.Strategy, repo.LineOf(ValidRepo.Strategy, "## Structure"), IssueCodes.PatternStructureNoClassDiagram));
    }

    [Fact]
    public void SeeItWithoutCompare()
    {
        var repo = Patterns()
            .Replace(ValidRepo.Strategy, "before title=", "title=")
            .Replace(ValidRepo.Strategy, "after title=", "title=");

        IssuesOf(repo).Should().Equal((ValidRepo.Strategy, repo.LineOf(ValidRepo.Strategy, "## See it"), IssueCodes.PatternSeeItNoCompare));
    }

    [Fact]
    public void OveruseMissing() =>
        IssuesOf(Patterns().Replace(ValidRepo.Strategy, "> [!OVERUSE]\n> One strategy forever.\n", string.Empty))
            .Should().Equal((ValidRepo.Strategy, FirstLine, IssueCodes.OveruseMissing));

    [Fact]
    public void OveruseTwice()
    {
        var repo = Patterns().Replace(ValidRepo.Strategy, "- `IComparer<T>`\n", "- `IComparer<T>`\n\n> [!OVERUSE]\n> Again.\n");

        IssuesOf(repo).Should().Equal((ValidRepo.Strategy, repo.LineOf(ValidRepo.Strategy, "> One strategy forever.") - 1, IssueCodes.OveruseDuplicate));
    }

    [Fact]
    public void OveruseCallout_IsAllowedInAnyLesson() =>
        IssuesOf(Patterns().Replace(ValidRepo.Auth, "## Token rotation\n", "## Token rotation\n\n> [!OVERUSE]\n> Too many tokens.\n"))
            .Should().BeEmpty();

    [Fact]
    public void SymbolMissing() =>
        IssuesOf(Patterns().Replace(ValidRepo.Strategy, "symbol: St\n", string.Empty))
            .Should().Equal((ValidRepo.Strategy, FrontMatterLine, IssueCodes.SymbolMissing));

    [Theory]
    [InlineData("ST")]
    [InlineData("S")]
    [InlineData("Str")]
    public void SymbolFormat(string symbol)
    {
        var repo = Patterns().Replace(ValidRepo.Strategy, "symbol: St", $"symbol: {symbol}");

        IssuesOf(repo).Should().Equal((ValidRepo.Strategy, repo.LineOf(ValidRepo.Strategy, "symbol:"), IssueCodes.SymbolFormat));
    }

    [Fact]
    public void SymbolDuplicate()
    {
        var repo = Patterns().Replace(ValidRepo.State, "symbol: Sa", "symbol: St");

        IssuesOf(repo).Should().Equal((ValidRepo.State, repo.LineOf(ValidRepo.State, "symbol:"), IssueCodes.SymbolDuplicate));
    }

    [Fact]
    public void ConceptRules_ApplyToPatternLessons() =>
        IssuesOf(Patterns().Replace(ValidRepo.Strategy, "hook: Swap how, keep what\n", string.Empty))
            .Should().Equal((ValidRepo.Strategy, FrontMatterLine, IssueCodes.ConceptHookMissing));

    [Fact]
    public void PatternRules_LeaveConceptModulesAlone() =>
        IssuesOf(Patterns().WithConceptModule()).Should().BeEmpty();

    private static ValidRepo Patterns() => new ValidRepo().WithPatternModule();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        new ContentPipeline(repo.Files(), new ContentOptions()).Build().Issues.Select(i => (i.Path, i.Line, i.Code));
}
