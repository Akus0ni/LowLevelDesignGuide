using FluentAssertions;
using Lld.Content.Authoring.Parsing;
using Lld.Content.Authoring.Validation;

namespace Lld.Content.Tests.Authoring;

public sealed class FrontMatterSchemaTests
{
    private static readonly FrontMatterSchema Schema = new(
        new FieldSpec("title", FieldKind.Text, Required: true),
        new FieldSpec("picker", FieldKind.Structured, Required: false));

    [Theory]
    [InlineData("title: A\npicker:\n  start: what\n")]
    [InlineData("title: A\npicker:\n  - patterns: [a, b]\n")]
    public void Structured_AcceptsNestedValues(string yaml) =>
        Schema.Check(Parse(yaml), "m.yml").Should().BeEmpty();

    [Theory]
    [InlineData("title: A\npicker: what\n")]
    [InlineData("title: A\npicker: [a, b]\n")]
    public void Structured_RejectsFlatValues(string yaml) =>
        Schema.Check(Parse(yaml), "m.yml").Select(i => (i.Line, i.Code)).Should().Equal((2, IssueCodes.FrontMatterWrongType));

    [Fact]
    public void OtherKinds_StillRejectNestedValues() =>
        Schema.Check(Parse("title:\n  nested: x\n"), "m.yml").Select(i => i.Code).Should().Equal(IssueCodes.FrontMatterWrongType);

    private static YamlMap Parse(string yaml) => YamlMapParser.Parse(yaml, "m.yml", firstLine: 1).Map!;
}
