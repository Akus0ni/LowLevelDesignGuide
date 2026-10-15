using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class SourceDocumentTests
{
    private const string Path = "content/m/l/lesson.md";

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void FrontMatter_IsReadWithFileLines_AndBodyStartsAfterIt(string newLine)
    {
        var text = string.Join(newLine, "---", "id: srp", "title: \"Single responsibility\"", "tags: [solid, srp]", "---", "# Title", "", "Body.");

        var (document, issue) = SourceDocument.Parse(Path, text);

        issue.Should().BeNull();
        document.FrontMatter.Should().NotBeNull();
        document.FrontMatter!.Line.Should().Be(2);
        document.FrontMatter["id"].Should().Be(new YamlEntry("id", 2, YamlValueKind.Scalar, "srp", []));
        document.FrontMatter["title"]!.Scalar.Should().Be("Single responsibility");
        document.FrontMatter["tags"]!.Kind.Should().Be(YamlValueKind.List);
        document.FrontMatter["tags"]!.Items.Should().Equal("solid", "srp");
        document.FrontMatter["tags"]!.Line.Should().Be(4);
        document.Body.Should().Be(string.Join(newLine, "# Title", "", "Body."));
    }

    [Fact]
    public void NoFrontMatter_BodyIsTheWholeFile()
    {
        var (document, issue) = SourceDocument.Parse(Path, "# Title\n\nBody.");

        issue.Should().BeNull();
        document.FrontMatter.Should().BeNull();
        document.Body.Should().Be("# Title\n\nBody.");
    }

    [Fact]
    public void BrokenYaml_IsAnIssueOnItsFileLine()
    {
        var (document, issue) = SourceDocument.Parse(Path, "---\nid: srp\ntitle: [unclosed\n---\nBody.");

        document.FrontMatter.Should().BeNull();
        issue.Should().NotBeNull();
        issue!.Code.Should().Be(YamlMapParser.InvalidCode);
        issue.Path.Should().Be(Path);
        issue.Line.Should().BeInRange(3, 4);
    }

    [Fact]
    public void FrontMatterThatIsNotAMap_IsAnIssue()
    {
        var (_, issue) = SourceDocument.Parse(Path, "---\n- a\n- b\n---\nBody.");

        issue!.Code.Should().Be(YamlMapParser.NotMappingCode);
        issue.Line.Should().Be(2);
    }

    [Fact]
    public void LineAt_MapsIndexesToOneBasedLines()
    {
        var (document, _) = SourceDocument.Parse(Path, "ab\ncd\r\nef");

        document.LineAt(0).Should().Be(1);
        document.LineAt(2).Should().Be(1);
        document.LineAt(3).Should().Be(2);
        document.LineAt(7).Should().Be(3);
    }
}
