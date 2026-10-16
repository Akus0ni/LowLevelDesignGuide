using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class FenceInfoTests
{
    [Fact]
    public void Parse_ReadsLanguageFlagsAndAttributes_InOrder()
    {
        var info = FenceInfo.Parse("csharp before title=\"Before: one class, three jobs\" snippet=\"samples/A.cs#A\" mark=3-5");

        info.Language.Should().Be("csharp");
        info.Flags.Should().Equal("before");
        info.IsBefore.Should().BeTrue();
        info.IsAfter.Should().BeFalse();
        info.Attributes.Select(a => a.Name).Should().Equal("title", "snippet", "mark");
        info[FenceInfo.TitleAttribute].Should().Be("Before: one class, three jobs");
        info[FenceInfo.MarkAttribute].Should().Be("3-5");
        info.Problems.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyInfo_HasNoLanguage(string? text)
    {
        var info = FenceInfo.Parse(text);

        info.Language.Should().BeEmpty();
        info.Flags.Should().BeEmpty();
        info.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void Parse_UnclosedQuote_IsAProblem_NotAnException()
    {
        var info = FenceInfo.Parse("csharp title=\"Before");

        info.Problems.Should().ContainSingle().Which.Should().Contain("title");
        info[FenceInfo.TitleAttribute].Should().BeNull();
    }

    [Fact]
    public void WithAndWithout_ChangeOnlyTheNamedAttribute()
    {
        var info = FenceInfo.Parse("csharp after snippet=\"samples/A.cs\" title=\"After\"");

        var packed = info.Without(FenceInfo.SnippetAttribute).With(FenceInfo.MarkAttribute, "2");

        packed.ToString().Should().Be("csharp after title=\"After\" mark=\"2\"");
        packed.With(FenceInfo.TitleAttribute, "New").ToString().Should().Be("csharp after title=\"New\" mark=\"2\"");
    }

    [Theory]
    [InlineData("3", new[] { 3 })]
    [InlineData("3-5,9", new[] { 3, 4, 5, 9 })]
    [InlineData(" 9 , 1-2, 2 ", new[] { 1, 2, 9 })]
    public void LineRanges_Parse(string text, int[] lines) =>
        LineRanges.Parse(text).Should().Equal(lines);

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("5-3")]
    [InlineData("1-2-3")]
    [InlineData("a")]
    [InlineData("-2")]
    public void LineRanges_Parse_RejectsInvalidText(string text) =>
        LineRanges.Parse(text).Should().BeNull();

    [Theory]
    [InlineData(new[] { 9, 3, 4, 5, 4 }, "3-5,9")]
    [InlineData(new[] { 1 }, "1")]
    [InlineData(new int[0], "")]
    public void LineRanges_Format_UsesTheShortestRanges(int[] lines, string text) =>
        LineRanges.Format(lines).Should().Be(text);
}
