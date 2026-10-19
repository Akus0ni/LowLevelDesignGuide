using FluentAssertions;
using Lld.Content.Authoring.Parsing;
using Lld.Contracts;

namespace Lld.Content.Tests.Authoring;

public sealed class FlowchartMapTests
{
    [Fact]
    public void Parse_ReadsGroupsInOrder_AndLooseNodesLast()
    {
        var map = FlowchartMap.Parse("""
            flowchart TD
              %% the pillars first
              subgraph pillars["The four pillars"]
                encapsulation["Encapsulation"]
                abstraction
              end
              subgraph solid["SOLID"]
                ocp["OCP"]
                lsp
              end
              dry-kiss-yagni["DRY, KISS & YAGNI"]
            """);

        map.IsFlowchart.Should().BeTrue();
        map.Groups.Should().BeEquivalentTo(
            new[]
            {
                new MapGroup("The four pillars", ["encapsulation", "abstraction"]),
                new MapGroup("SOLID", ["ocp", "lsp"]),
                new MapGroup(string.Empty, ["dry-kiss-yagni"]),
            },
            o => o.WithStrictOrdering());
        map.Nodes.Select(n => (n.Id, n.Line)).Should().Equal(
            ("encapsulation", 4), ("abstraction", 5), ("ocp", 8), ("lsp", 9), ("dry-kiss-yagni", 11));
    }

    [Fact]
    public void Parse_ReadsLinks_WithLabels_ChainsAndGroupsOfNodes()
    {
        var map = FlowchartMap.Parse("""
            graph LR
              ocp -->|"needs"| lsp --> dip
              srp & isp -.->|tempers| dip
              a["A & B"] ==> b
            """);

        map.Links.Should().Equal(
            new MapLink("ocp", "lsp", "needs"),
            new MapLink("lsp", "dip", null),
            new MapLink("srp", "dip", "tempers"),
            new MapLink("isp", "dip", "tempers"),
            new MapLink("a", "b", null));
        map.Groups.Should().ContainSingle().Which.Lessons.Should().Equal("ocp", "lsp", "dip", "srp", "isp", "a", "b");
    }

    [Theory]
    [InlineData("sequenceDiagram\n  a->>b: hi")]
    [InlineData("")]
    [InlineData("%% only a comment")]
    public void Parse_OtherDiagrams_AreNotFlowcharts(string source) =>
        FlowchartMap.Parse(source).IsFlowchart.Should().BeFalse();
}
