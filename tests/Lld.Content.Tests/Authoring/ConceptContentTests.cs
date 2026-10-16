using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Validation;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// Spec 004: snippets, Before / After, callouts, interview questions and module maps. Each validation test breaks one
/// thing in <see cref="ValidRepo.WithConceptModule"/> and expects exactly one issue (path, line, code).
/// </summary>
public sealed class ConceptContentTests
{
    private const string Fence = "```csharp snippet=\"samples/Srp.cs#Report.Save\"";
    private const int FenceLine = 18;
    private const int BeforeLine = 13;
    private const int AuthCodeLine = 20;

    [Fact]
    public void ConceptRepo_HasNoIssues()
    {
        var result = Build(Concept());

        result.Issues.Should().BeEmpty();
        result.Dependencies.Should().Equal(ValidRepo.Sample);
    }

    [Theory]
    [InlineData("```csharp\n", "```csharp beforee\n", IssueCodes.FenceUnknownFlag)]
    [InlineData("```csharp\n", "```csharp lines=\"2\"\n", IssueCodes.FenceUnknownAttribute)]
    [InlineData("```csharp\n", "```csharp mark=\"2-1\"\n", IssueCodes.FenceBadMark)]
    [InlineData("```csharp\n", "```csharp title=\"Oops\n", IssueCodes.FenceSyntax)]
    public void FenceInfo(string oldText, string newText, string code) =>
        IssuesOf(Concept().Replace(ValidRepo.Auth, oldText, newText)).Should().Equal((ValidRepo.Auth, AuthCodeLine, code));

    [Fact]
    public void MarkOnACompareFence() =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, "before title=", "before mark=\"1\" title="))
            .Should().Equal((ValidRepo.Srp, BeforeLine, IssueCodes.FenceMarkOnCompare));

    [Theory]
    [InlineData(Fence, "```ts snippet=\"samples/Srp.cs#Report.Save\"", IssueCodes.SnippetNotCSharp)]
    [InlineData(Fence + "\n```", Fence + "\nvar x = 1;\n```", IssueCodes.SnippetHasCode)]
    [InlineData("samples/Srp.cs#Report.Save", "samples/Srp.txt#Report.Save", IssueCodes.SnippetBadReference)]
    [InlineData("samples/Srp.cs#Report.Save", "samples/Nope.cs#Report.Save", IssueCodes.SnippetFileMissing)]
    [InlineData("samples/Srp.cs#Report.Save", "samples/Srp.cs#Report.Delete", IssueCodes.SnippetSymbolMissing)]
    public void Snippet(string oldText, string newText, string code) =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, oldText, newText)).Should().Equal((ValidRepo.Srp, FenceLine, code));

    [Fact]
    public void AmbiguousSnippet() =>
        IssuesOf(Concept().Replace(ValidRepo.Sample, "public sealed class ReportRenderer", "public sealed class Outer\n{\n    public sealed class Report;\n}\n\npublic sealed class ReportRenderer"))
            .Should().Equal((ValidRepo.Srp, BeforeLine, IssueCodes.SnippetSymbolAmbiguous));

    [Fact]
    public void BeforeWithoutAfter() =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, "```csharp after title", "```csharp title"))
            .Should().Equal((ValidRepo.Srp, BeforeLine, IssueCodes.CompareUnpaired));

    [Fact]
    public void BeforeAndAfterInDifferentLanguages() =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, "```csharp after", "```cs after"))
            .Should().Equal((ValidRepo.Srp, BeforeLine + 2, IssueCodes.CompareLanguageMismatch));

    [Fact]
    public void UnknownCallout() =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, "[!SMELL]", "[!SMEL]"))
            .Should().Equal((ValidRepo.Srp, 10, IssueCodes.CalloutUnknownType));

    [Fact]
    public void ConceptLessonWithoutInterviewQuestions() =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, ValidRepo.InterviewSection(5), "## Summary\n\nDone.\n"))
            .Should().Equal((ValidRepo.Ocp, 1, IssueCodes.InterviewMissing));

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    public void TooFewOrTooManyQuestions(int count) =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, ValidRepo.InterviewSection(5), ValidRepo.InterviewSection(count)))
            .Should().Equal((ValidRepo.Ocp, 10, IssueCodes.InterviewCount));

    [Fact]
    public void QuestionWithoutAnswer() =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, "### Question 2?\n\nAnswer 2.\n\n", "### Question 2?\n\n"))
            .Should().Equal((ValidRepo.Ocp, 16, IssueCodes.InterviewNoAnswer));

    [Fact]
    public void InterviewQuestions_AreOnlyRequiredInConceptModules() =>
        IssuesOf(new ValidRepo()).Should().BeEmpty();

    [Fact]
    public void InlineCSharpInAConceptCheatSheet() =>
        IssuesOf(Concept().Replace(ValidRepo.SrpSheet, "One reason to change.\n", "One reason to change.\n\n```csharp\nvar x = 1;\n```\n"))
            .Should().Equal((ValidRepo.SrpSheet, 10, IssueCodes.CodeInlineCSharp));

    [Fact]
    public void MapThatIsNotAFlowchart() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "flowchart LR", "sequenceDiagram"))
            .Should().Equal((ValidRepo.ConceptModule, 5, IssueCodes.MapNotFlowchart));

    [Fact]
    public void MapWithAnUnknownNode() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "| ocp[\"OCP\"]", "| ocp[\"OCP\"] --> dip"))
            .Should().Equal((ValidRepo.ConceptModule, 7, IssueCodes.MapUnknownNode));

    [Fact]
    public void MapWithoutALesson() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, " -->|\"leads to\"| ocp[\"OCP\"]", string.Empty))
            .Should().Equal((ValidRepo.ConceptModule, 5, IssueCodes.MapLessonMissing));

    [Fact]
    public void UnknownTemplate() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "template: concept", "template: case"))
            .Should().Equal((ValidRepo.ConceptModule, 4, IssueCodes.FrontMatterWrongType));

    [Fact]
    public void Pack_InlinesSnippets_AndMarksWhatChanged()
    {
        var srp = Pack().Modules.Single(m => m.Slug == "oop-solid").Lessons.Single(l => l.Slug == "srp");

        srp.BodyMarkdown.Should().Contain("""
            ```csharp before title="Before" mark="1,5"
            public sealed class Report
            {
                public string Render() => "report";

                public void Save() { }
            }
            ```
            ```csharp after title="After" mark="1"
            public sealed class ReportRenderer
            {
                public string Render() => "report";
            }
            ```

            ```csharp
            public void Save() { }
            ```
            """.ReplaceLineEndings("\n"));
        srp.BodyMarkdown.Should().NotContain("snippet=");
    }

    [Fact]
    public void Pack_CarriesTheMap_AndKeepsQuestionsOutOfTheOutline()
    {
        var module = Pack().Modules.Single(m => m.Slug == "oop-solid");

        module.Map.Should().BeEquivalentTo(
            new ModuleMap([new MapGroup(string.Empty, ["srp", "ocp"])], [new MapLink("srp", "ocp", "leads to")]),
            o => o.WithStrictOrdering());
        module.Lessons[0].Outline.Should().Equal(new OutlineEntry(2, "Interview questions", "interview-questions"));
        Pack().Modules.Single(m => m.Slug == "start").Map.Should().BeNull();
    }

    private static ValidRepo Concept() => new ValidRepo().WithConceptModule();

    private static ContentPack Pack()
    {
        var result = Build(Concept());
        result.Issues.Should().BeEmpty();
        return result.Pack!;
    }

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
