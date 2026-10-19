using FluentAssertions;
using Lld.Content.Authoring.Editing;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class CodeFencesTests
{
    private const string Path = "content/oop-solid/srp/lesson.md";

    [Fact]
    public void Of_ListsFences_WithLineInfoAndCode()
    {
        var fences = CodeFences.Of(Parse("# T\n\n```csharp title=\"A\"\nvar a = 1;\nvar b = 2;\n```\n\n```mermaid\nflowchart LR\n```\n"));

        fences.Select(f => (f.Info.Language, f.Line, f.Code)).Should().Equal(
            ("csharp", 3, "var a = 1;\nvar b = 2;"),
            ("mermaid", 8, "flowchart LR"));
    }

    [Fact]
    public void Compare_PairsABeforeWithTheAfterRightBelowIt()
    {
        var layout = Layout("```csharp before\na\n```\n```csharp after\nb\n```\n");

        layout.Pairs.Should().ContainSingle();
        layout.Unpaired.Should().BeEmpty();
    }

    [Theory]
    [InlineData("```csharp before\na\n```\n\nText between.\n\n```csharp after\nb\n```\n")]
    [InlineData("```csharp after\nb\n```\n```csharp before\na\n```\n")]
    [InlineData("```csharp before after\na\n```\n")]
    [InlineData("- item\n\n  ```csharp before\n  a\n  ```\n\n```csharp after\nb\n```\n")]
    public void Compare_ReportsFencesWithoutAPartner(string markdown)
    {
        var layout = Layout(markdown);

        layout.Pairs.Should().BeEmpty();
        layout.Unpaired.Should().NotBeEmpty();
    }

    [Fact]
    public void BodyComposer_AppliesEditsFromTheEnd_AndDropsTheFrontMatter()
    {
        var source = Parse("---\nid: x\n---\nabc def");
        var body = source.BodyStart;

        var text = BodyComposer.Compose(source, [new TextEdit(body, 3, "ABC!"), new TextEdit(body + 4, 3, "D")]);

        text.Should().Be("ABC! D");
    }

    [Fact]
    public void BodyComposer_RejectsOverlappingEdits()
    {
        var source = Parse("abc def");

        var compose = () => BodyComposer.Compose(source, [new TextEdit(0, 5, "x"), new TextEdit(4, 2, "y")]);

        compose.Should().Throw<InvalidOperationException>().WithMessage("*overlaps*");
    }

    private static CompareLayout Layout(string markdown) => CodeFences.Compare(CodeFences.Of(Parse(markdown)));

    private static SourceDocument Parse(string text) => SourceDocument.Parse(Path, text).Document;
}
