using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// Spec 008 §3.2, §3.6, §3.7, §3.8: the <c>diagram</c> template, the diagram fence keys and the picker changes. Each test
/// breaks one thing in <see cref="ValidRepo.WithDiagramModule"/> and expects exactly the issues it causes.
/// </summary>
public sealed class DiagramContentTests
{
    private const int FirstLine = 1;
    private const int FrontMatterLine = 2;

    [Fact]
    public void DiagramRepo_HasNoIssues() => IssuesOf(Diagrams()).Should().BeEmpty();

    [Theory]
    [InlineData("## The idea\n\nWhat it shows.\n\n")]
    [InlineData("## In an interview\n\nDraw it first.\n\n")]
    public void MissingCommonSection(string section) =>
        IssuesOf(Diagrams().Replace(ValidRepo.ClassBasics, section, string.Empty))
            .Should().Equal((ValidRepo.ClassBasics, FirstLine, IssueCodes.DiagramSectionMissing));

    [Fact]
    public void MissingNotation() =>
        IssuesOf(Diagrams().Replace(ValidRepo.ClassBasics, "## Notation\n", "### Symbols\n"))
            .Should().Equal((ValidRepo.ClassBasics, FirstLine, IssueCodes.DiagramSectionMissing));

    [Fact]
    public void MissingCodeAndRequirements() =>
        IssuesOf(Diagrams().Replace(ValidRepo.ClassBasics, "## Code ↔ UML\n", "### Pair\n"))
            .Should().Equal((ValidRepo.ClassBasics, FirstLine, IssueCodes.DiagramSectionMissing));

    [Fact]
    public void InterviewLesson_NeedsBothOfItsSections() =>
        IssuesOf(Diagrams().Replace(ValidRepo.Interviews, "## Worked example\n", "### Example\n"))
            .Should().Equal((ValidRepo.Interviews, FirstLine, IssueCodes.DiagramSectionMissing));

    [Fact]
    public void ReadItWithoutDiagram()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "```mermaid\nclassDiagram\n  Order --> Customer\n```", "Order uses Customer.");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "## Read it"), IssueCodes.DiagramSectionNoDiagram));
    }

    [Fact]
    public void FromRequirementsWithoutDiagram()
    {
        var repo = Diagrams().Replace(ValidRepo.UseCase,
            "```mermaid\nflowchart TD\n  learner[\"🧍 Learner\"] --> ask([\"Request access\"])\n```", "A box.");

        IssuesOf(repo).Should().Equal((ValidRepo.UseCase, repo.LineOf(ValidRepo.UseCase, "## From requirements"), IssueCodes.DiagramSectionNoDiagram));
    }

    [Fact]
    public void CodeUmlWithoutCodeAttribute()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "```mermaid code=\"samples/Uml.cs#Order,OrderLine,Customer\"", "```mermaid");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "## Code ↔ UML"), IssueCodes.DiagramSectionNoDiagram));
    }

    [Fact]
    public void MistakesMissing() =>
        IssuesOf(Diagrams().Replace(ValidRepo.ClassBasics, "> [!MISTAKES]\n> - Arrows the wrong way.\n", string.Empty))
            .Should().Equal((ValidRepo.ClassBasics, FirstLine, IssueCodes.MistakesMissing));

    [Fact]
    public void MistakesTwice()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "What it shows.\n", "What it shows.\n\n> [!MISTAKES]\n> - Again.\n");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "> - Arrows the wrong way.") - 1, IssueCodes.MistakesDuplicate));
    }

    [Fact]
    public void ConceptRules_ApplyToDiagramLessons() =>
        IssuesOf(Diagrams().Replace(ValidRepo.ClassBasics, "hook: Boxes and arrows\n", string.Empty))
            .Should().Equal((ValidRepo.ClassBasics, FrontMatterLine, IssueCodes.ConceptHookMissing));

    [Fact]
    public void DiagramRules_LeaveOtherModulesAlone() =>
        IssuesOf(Diagrams().WithConceptModule().WithPatternModule()).Should().BeEmpty();

    [Fact]
    public void CodeAttribute_OnlyOnMermaid()
    {
        var repo = Diagrams().Replace(ValidRepo.Auth, "```csharp\npublic sealed class AccessRequest;", "```csharp code=\"samples/Uml.cs#Order\"\npublic sealed class AccessRequest;");

        IssuesOf(repo).Should().Equal((ValidRepo.Auth, repo.LineOf(ValidRepo.Auth, "```csharp code="), IssueCodes.FenceDiagramKeyNotMermaid));
    }

    [Fact]
    public void StepsFlag_OnAFlowchart_IsFine() =>
        IssuesOf(Diagrams().Replace(ValidRepo.UseCase, "## Read it\n\n```mermaid\n", "## Read it\n\n```mermaid steps\n")).Should().BeEmpty();

    [Fact]
    public void StepsFlag_NotOnAFlowchart()
    {
        var repo = Diagrams().Replace(ValidRepo.Auth, "```mermaid\nstateDiagram-v2", "```mermaid steps\nstateDiagram-v2");

        IssuesOf(repo).Should().Equal((ValidRepo.Auth, repo.LineOf(ValidRepo.Auth, "```mermaid steps"), IssueCodes.FenceStepsNotFlowchart));
    }

    [Fact]
    public void PackerOutputKeys_AreNotForAuthors()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "## Read it\n\n```mermaid\n", "## Read it\n\n```mermaid pair links=\"Order=1\"\n");
        var line = repo.LineOf(ValidRepo.ClassBasics, "```mermaid pair");

        IssuesOf(repo).Should().Equal(
            (ValidRepo.ClassBasics, line, IssueCodes.FenceUnknownAttribute),
            (ValidRepo.ClassBasics, line, IssueCodes.FenceUnknownFlag));
    }

    [Fact]
    public void Picker_OutsidePatternModules_MayLeaveLessonsOut()
    {
        var result = Build(Diagrams());

        result.Issues.Should().BeEmpty();
        result.Pack!.Modules.Single(m => m.Slug == "uml").Picker!.Title.Should().Be("Which diagram?");
    }

    [Fact]
    public void Picker_TitleTooLong()
    {
        var repo = Diagrams().Replace(ValidRepo.DiagramModule, "title: \"Which diagram?\"", $"title: \"{new string('x', 41)}\"");

        IssuesOf(repo).Should().Equal((ValidRepo.DiagramModule, repo.LineOf(ValidRepo.DiagramModule, "  title:"), IssueCodes.PickerShape));
    }

    [Theory]
    [InlineData("samples/Uml.cs#Order,OrderLine,Customer", "samples/Missing.cs#Order")]
    [InlineData("samples/Uml.cs#Order,OrderLine,Customer", "samples/Uml.cs#Order,Ghost")]
    [InlineData("samples/Uml.cs#Order,OrderLine,Customer", "Uml.cs")]
    public void CodePair_Unresolved(string oldReference, string newReference)
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, oldReference, newReference);

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "```mermaid code="), IssueCodes.CodePairUnresolved));
    }

    [Fact]
    public void CodePair_OnAFlowchart()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "classDiagram\n  Order *-- OrderLine\n  Order --> Customer", "flowchart TD\n  a --> b");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "```mermaid code="), IssueCodes.CodePairNotSupported));
    }

    [Fact]
    public void CodePair_QuotingOnlyAMember()
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "samples/Uml.cs#Order,OrderLine,Customer", "samples/Uml.cs#Order.Add");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, "```mermaid code="), IssueCodes.CodePairNoTypes));
    }

    [Theory]
    [InlineData("Order --> Ghost")]
    [InlineData("Customer <|-- Order")]
    [InlineData("Customer --> Order")]
    public void CodePair_DiagramDoesNotMatchTheCode(string badLine)
    {
        var repo = Diagrams().Replace(ValidRepo.ClassBasics, "  Order *-- OrderLine\n  Order --> Customer\n", $"  Order *-- OrderLine\n  Order --> Customer\n  {badLine}\n");

        IssuesOf(repo).Should().Equal((ValidRepo.ClassBasics, repo.LineOf(ValidRepo.ClassBasics, badLine), IssueCodes.CodePairMismatch));
    }

    [Fact]
    public void CodePair_IsCheckedInAnyTemplate()
    {
        var repo = Diagrams().Replace(ValidRepo.Auth, "```mermaid\nstateDiagram-v2\n  Pending --> Approved\n```",
            "```mermaid code=\"samples/Uml.cs#Order\"\nclassDiagram\n  Order --> Ghost\n```");

        IssuesOf(repo).Should().Equal((ValidRepo.Auth, repo.LineOf(ValidRepo.Auth, "  Order --> Ghost"), IssueCodes.CodePairMismatch));
    }

    private static ValidRepo Diagrams() => new ValidRepo().WithDiagramModule();

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
