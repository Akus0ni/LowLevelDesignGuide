using FluentAssertions;
using Lld.Content.Authoring.Parsing;

namespace Lld.Content.Tests.Authoring;

public sealed class YamlMapParserTests
{
    [Fact]
    public void ReadsScalarsListsAndOtherShapes_WithLinesFromTheFirstLine()
    {
        var (map, issue) = YamlMapParser.Parse("title: OOP\norder: 1\ntags:\n  - a\n  - b\nnested:\n  key: value\nempty:\n", "m.yml", firstLine: 10);

        issue.Should().BeNull();
        map!.Entries.Select(e => (e.Key, e.Line, e.Kind)).Should().Equal(
            ("title", 10, YamlValueKind.Scalar),
            ("order", 11, YamlValueKind.Scalar),
            ("tags", 12, YamlValueKind.List),
            ("nested", 15, YamlValueKind.Other),
            ("empty", 17, YamlValueKind.Scalar));
        map["order"]!.Scalar.Should().Be("1");
        map["tags"]!.Items.Should().Equal("a", "b");
        map["empty"]!.Scalar.Should().BeEmpty();
        map["missing"].Should().BeNull();
    }

    [Fact]
    public void EmptyText_IsAnEmptyMap()
    {
        var (map, issue) = YamlMapParser.Parse(string.Empty, "m.yml", firstLine: 1);

        issue.Should().BeNull();
        map!.Entries.Should().BeEmpty();
    }

    [Fact]
    public void DuplicateKey_IsAnIssue()
    {
        var (map, issue) = YamlMapParser.Parse("title: A\ntitle: B\n", "m.yml", firstLine: 1);

        map.Should().BeNull();
        issue!.Code.Should().Be(YamlMapParser.InvalidCode);
        issue.Line.Should().Be(2);
    }
}
