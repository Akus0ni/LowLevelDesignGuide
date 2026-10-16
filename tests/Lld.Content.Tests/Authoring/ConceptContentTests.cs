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
    private const int FenceLine = 23;
    private const int BeforeLine = 18;
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
            .Should().Equal((ValidRepo.Srp, 15, IssueCodes.CalloutUnknownType));

    [Fact]
    public void ConceptLessonWithoutInterviewQuestions() =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, ValidRepo.InterviewSection(5), "## Summary\n\nDone.\n"))
            .Should().Equal((ValidRepo.Ocp, 1, IssueCodes.InterviewMissing));

    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    public void TooFewOrTooManyQuestions(int count) =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, ValidRepo.InterviewSection(5), ValidRepo.InterviewSection(count)))
            .Should().Equal((ValidRepo.Ocp, 15, IssueCodes.InterviewCount));

    [Fact]
    public void QuestionWithoutAnswer() =>
        IssuesOf(Concept().Replace(ValidRepo.Ocp, "### Question 2?\n\nAnswer 2.\n\n", "### Question 2?\n\n"))
            .Should().Equal((ValidRepo.Ocp, 21, IssueCodes.InterviewNoAnswer));

    [Fact]
    public void InterviewQuestions_AreOnlyRequiredInConceptModules() =>
        IssuesOf(new ValidRepo()).Should().BeEmpty();

    [Fact]
    public void InlineCSharpInAConceptCheatSheet() =>
        IssuesOf(Concept().Replace(ValidRepo.SrpSheet, "One reason to change.\n", "One reason to change.\n\n```csharp\nvar x = 1;\n```\n"))
            .Should().Equal((ValidRepo.SrpSheet, 13, IssueCodes.CodeInlineCSharp));

    [Fact]
    public void MapThatIsNotAFlowchart() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "flowchart LR", "sequenceDiagram"))
            .Should().Equal((ValidRepo.ConceptModule, 6, IssueCodes.MapNotFlowchart));

    [Fact]
    public void MapWithAnUnknownNode() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "| ocp[\"OCP\"]", "| ocp[\"OCP\"] --> dip"))
            .Should().Equal((ValidRepo.ConceptModule, 8, IssueCodes.MapUnknownNode));

    [Fact]
    public void MapWithoutALesson() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, " -->|\"leads to\"| ocp[\"OCP\"]", string.Empty))
            .Should().Equal((ValidRepo.ConceptModule, 6, IssueCodes.MapLessonMissing));

    [Fact]
    public void UnknownTemplate() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "template: concept", "template: case"))
            .Should().Equal((ValidRepo.ConceptModule, 4, IssueCodes.FrontMatterWrongType));

    [Fact]
    public void LessonWithoutARememberStamp() =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, "> [!REMEMBER]\n> One job.\n\n", string.Empty))
            .Should().Equal((ValidRepo.Srp, 1, IssueCodes.RememberMissing));

    [Fact]
    public void CheatSheetWhoseStampComesAfterTheFirstSection() =>
        IssuesOf(Concept().Replace(ValidRepo.OcpSheet, "> [!REMEMBER]\n> Extend.\n\n## In one line\n\nExtend.\n", "## In one line\n\n> [!REMEMBER]\n> Extend.\n"))
            .Should().Equal((ValidRepo.OcpSheet, 1, IssueCodes.RememberMissing));

    [Theory]
    [InlineData("icon: \"🎯\"\n", "", 2, IssueCodes.ConceptIconMissing)]
    [InlineData("icon: \"🎯\"", "icon: \"🎯🎯\"", 7, IssueCodes.ConceptIconMissing)]
    [InlineData("hook: One reason to change\n", "", 2, IssueCodes.ConceptHookMissing)]
    [InlineData("hook: One reason to change", "hook: One reason to change, and never two, not even on Fridays", 8, IssueCodes.ConceptHookTooLong)]
    public void ConceptLessonWithoutAFace(string oldText, string newText, int line, string code) =>
        IssuesOf(Concept().Replace(ValidRepo.Srp, oldText, newText)).Should().Equal((ValidRepo.Srp, line, code));

    [Theory]
    [InlineData("SO srp ocp", IssueCodes.MnemonicFormat)]
    [InlineData("SOL: srp, ocp", IssueCodes.MnemonicFormat)]
    [InlineData("SO: srp, lsp", IssueCodes.MnemonicUnknownLesson)]
    public void BrokenMnemonic(string mnemonic, string code) =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "SO: srp, ocp", mnemonic))
            .Should().Equal((ValidRepo.ConceptModule, 5, code));

    [Fact]
    public void MnemonicLettersMustStartTheTitles() =>
        IssuesOf(Concept().Replace(ValidRepo.ConceptModule, "SO: srp, ocp", "OS: srp, ocp")).Should().Equal(
            (ValidRepo.ConceptModule, 5, IssueCodes.MnemonicLetterMismatch),
            (ValidRepo.ConceptModule, 5, IssueCodes.MnemonicLetterMismatch));

    [Fact]
    public void Pack_CarriesIconsHooksAndMnemonics()
    {
        var module = Pack().Modules.Single(m => m.Slug == "oop-solid");

        module.Lessons.Select(l => (l.Icon, l.Hook)).Should().Equal(("🎯", "One reason to change"), ("➕", "Add, don't edit"));
        module.Mnemonics.Should().BeEquivalentTo([new Mnemonic("SO", ["srp", "ocp"])], o => o.WithStrictOrdering());
    }

    [Fact]
    public void GoDeeper_IsLeftOutOfReadingTimeAndOutline()
    {
        var longRead = string.Join(' ', Enumerable.Repeat("word", 2000));
        var repo = Concept().Replace(ValidRepo.Ocp, "Answer 5.\n\n", $"Answer 5.\n\n## Go deeper\n\n### A second example\n\n{longRead}\n");

        var ocp = Pack(repo).Modules.Single(m => m.Slug == "oop-solid").Lessons.Single(l => l.Slug == "ocp");

        ocp.ReadingMinutes.Should().Be(1);
        ocp.Outline.Select(o => o.Text).Should().Equal("Interview questions", "Go deeper");
        ocp.BodyMarkdown.Should().Contain("### A second example");
    }

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

    private static ContentPack Pack(ValidRepo? repo = null)
    {
        var result = Build(repo ?? Concept());
        result.Issues.Should().BeEmpty();
        return result.Pack!;
    }

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
