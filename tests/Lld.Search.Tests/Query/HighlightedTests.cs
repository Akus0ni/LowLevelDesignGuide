using FluentAssertions;
using Lld.Search.Query;

namespace Lld.Search.Tests.Query;

public sealed class HighlightedTests
{
    private const string O = Highlighted.Open;
    private const string C = Highlighted.Close;

    [Fact]
    public void Snippet_TurnsMarkersIntoRanges()
    {
        var snippet = Highlighted.Snippet($"…a class {O}inherits{C} from {O}Base{C}…");

        snippet.Text.Should().Be("…a class inherits from Base…");
        snippet.Marks.Select(mark => (mark[0], mark[1])).Should().Equal((9, 8), (23, 4));
    }

    [Fact]
    public void Snippet_KeepsAtMostThreeMarks()
    {
        var snippet = Highlighted.Snippet($"{O}a{C} {O}b{C} {O}c{C} {O}d{C}");

        snippet.Text.Should().Be("a b c d");
        snippet.Marks.Should().HaveCount(Highlighted.MaxSnippetMarks);
    }

    [Fact]
    public void Snippet_DropsMarkdownMarkersFromNotes()
    {
        var snippet = Highlighted.Snippet($"#interview - **Situation:** `{O}airplane{C}` mode");

        snippet.Text.Should().Be("#interview - Situation: airplane mode");
        snippet.Marks.Select(mark => snippet.Text.Substring(mark[0], mark[1])).Should().Equal("airplane");
    }

    [Fact]
    public void Snippet_IgnoresAnUnmatchedClose() =>
        Highlighted.Snippet($"x{C}y").Should().BeEquivalentTo(new { Text = "xy", Marks = Array.Empty<int[]>() });

    [Fact]
    public void Words_ReturnsMarkedTextAsWritten() =>
        Highlighted.Words($"{O}Inheritance{C}: a class {O}inherits{C}").Should().Equal("Inheritance", "inherits");
}
