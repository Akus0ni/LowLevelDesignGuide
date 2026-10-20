using FluentAssertions;
using Lld.Content.Authoring;
using Lld.Content.Authoring.Validation;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

/// <summary>
/// Spec 006 §3.6, §3.7: the "Which pattern?" picker and look-alike groups in <c>module.yml</c>. Each test breaks one
/// thing in <see cref="ValidRepo.WithPatternModule"/> and expects the issues it causes (path, line, code).
/// </summary>
public sealed class PickerContentTests
{
    private const string Module = ValidRepo.PatternModule;
    private const string ObjectAnswer = "{ label: \"The object\", next: inside }";
    private const string YesAnswer = "{ label: \"Yes\", pick: [state], why: \"Each state is a class.\" }";
    private const string NoAnswer = "{ label: \"No\", pick: [strategy], why: \"Still a strategy.\" }";

    [Fact]
    public void Pack_CarriesPickerLookalikesAndSymbols()
    {
        var result = Build(Patterns());

        result.Issues.Should().BeEmpty();
        var module = result.Pack!.Modules.Single(m => m.Slug == "patterns");
        module.Picker!.Start.Should().Be("what");
        module.Picker.Questions.Select(q => q.Id).Should().Equal("what", "inside");
        module.Picker.Questions[0].Answers.Should().BeEquivalentTo(
            [new PickerAnswer("The caller", null, ["strategy"], "Pass the algorithm in."), new PickerAnswer("The object", "inside", [], null)],
            o => o.WithStrictOrdering());
        module.Lookalikes.Should().BeEquivalentTo(
            [new Lookalike(["strategy", "state"], "Strategy is chosen by the caller; State switches itself.", "Who swaps the behaviour?")]);
        module.Lessons.Select(l => l.Symbol).Should().Equal("St", "Sa");
        result.Pack.Modules.Single(m => m.Slug == "start").Picker.Should().BeNull();
    }

    [Theory]
    [InlineData(ObjectAnswer, "{ label: \"The object\", next: inside, pick: [state], why: \"x\" }")]
    [InlineData(ObjectAnswer, "{ label: \"The object\" }")]
    [InlineData(ObjectAnswer, "{ label: \"The object\", next: inside, colour: red }")]
    [InlineData(YesAnswer, "{ label: \"Yes\", pick: [state] }")]
    [InlineData(YesAnswer, "{ label: \"Yes\", pick: [state, strategy, visitor], why: \"x\" }")]
    public void BadAnswer(string oldText, string newText)
    {
        var repo = Patterns().Replace(Module, oldText, newText);

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, newText), IssueCodes.PickerShape));
    }

    [Fact]
    public void QuestionWithOneAnswer()
    {
        var repo = Patterns().Replace(Module, $"\n        - {NoAnswer}", string.Empty);

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, YesAnswer), IssueCodes.PickerShape));
    }

    [Fact]
    public void NextToAMissingQuestion()
    {
        var repo = Patterns().Replace(Module, "next: inside", "next: nowhere");

        IssuesOf(repo).Should().Equal(
            (Module, repo.LineOf(Module, "picker:"), IssueCodes.PickerUnreachable),
            (Module, repo.LineOf(Module, "next: nowhere"), IssueCodes.PickerUnknownNext),
            (Module, repo.LineOf(Module, "inside:"), IssueCodes.PickerUnreachable));
    }

    [Fact]
    public void Loop()
    {
        var repo = Patterns().Replace(Module, NoAnswer, "{ label: \"No\", next: what }");

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "next: what"), IssueCodes.PickerCycle));
    }

    [Fact]
    public void UnknownLesson()
    {
        var repo = Patterns().Replace(Module, "pick: [state]", "pick: [visitor]");

        IssuesOf(repo).Should().Equal(
            (Module, repo.LineOf(Module, "picker:"), IssueCodes.PickerUnreachable),
            (Module, repo.LineOf(Module, "pick: [visitor]"), IssueCodes.PickerUnknownLesson));
    }

    [Fact]
    public void LessonNobodyPicks()
    {
        var repo = Patterns().Replace(Module, "pick: [state]", "pick: [strategy]");

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "picker:"), IssueCodes.PickerUnreachable));
    }

    [Fact]
    public void PathLongerThanFourQuestions()
    {
        var chain = string.Concat(Enumerable.Range(1, 4).Select(i =>
            $"    q{i}:\n      ask: \"Step {i}?\"\n      answers:\n        - {{ label: \"On\", next: q{i + 1} }}\n        - {{ label: \"Stop\", pick: [strategy], why: \"Enough.\" }}\n"));
        var last = "    q5:\n      ask: \"Last?\"\n      answers:\n        - { label: \"Yes\", pick: [state], why: \"Finally.\" }\n        - { label: \"No\", pick: [strategy], why: \"Fine.\" }\n";
        var repo = Patterns().Set(Module, $"title: Patterns\norder: 3\nsummary: Named designs.\ntemplate: pattern\npicker:\n  start: q1\n  questions:\n{chain}{last}");

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "q5:"), IssueCodes.PickerTooDeep));
    }

    [Fact]
    public void PickerWrittenAsText() =>
        IssuesOf(new ValidRepo().WithPatternModule().Set(Module, "title: Patterns\norder: 3\nsummary: Named designs.\ntemplate: pattern\npicker: yes\n"))
            .Should().Equal((Module, 5, IssueCodes.FrontMatterWrongType));

    [Theory]
    [InlineData("patterns: [strategy, state]", "patterns: [strategy]", IssueCodes.LookalikeShape)]
    [InlineData("patterns: [strategy, state]", "patterns: [strategy, visitor]", IssueCodes.LookalikeUnknownLesson)]
    [InlineData("patterns: [strategy, state]", "patterns: [strategy, strategy]", IssueCodes.LookalikeDuplicate)]
    [InlineData("patterns: [strategy, state]", "patterns: [strategy, state]\n    colour: red", IssueCodes.LookalikeShape)]
    [InlineData("    ask: \"Who swaps the behaviour?\"", "", IssueCodes.LookalikeShape)]
    public void BadLookalike(string oldText, string newText, string code)
    {
        var repo = Patterns().Replace(Module, oldText, newText);

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "- patterns:"), code));
    }

    [Fact]
    public void LookalikeTextTooLong()
    {
        var repo = Patterns().Replace(Module, "Who swaps the behaviour?", new string('a', 161));

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "- patterns:"), IssueCodes.LookalikeShape));
    }

    [Fact]
    public void SameLookalikeTwice()
    {
        var repo = Patterns().Replace(Module, "    ask: \"Who swaps the behaviour?\"",
            "    ask: \"Who swaps the behaviour?\"\n  - patterns: [state, strategy]\n    difference: \"Again.\"\n    ask: \"Again?\"");

        IssuesOf(repo).Should().Equal((Module, repo.LineOf(Module, "- patterns: [state"), IssueCodes.LookalikeDuplicate));
    }

    private static ValidRepo Patterns() => new ValidRepo().WithPatternModule();

    private static ContentBuildResult Build(ValidRepo repo) => new ContentPipeline(repo.Files(), new ContentOptions()).Build();

    private static IEnumerable<(string Path, int? Line, string Code)> IssuesOf(ValidRepo repo) =>
        Build(repo).Issues.Select(i => (i.Path, i.Line, i.Code));
}
