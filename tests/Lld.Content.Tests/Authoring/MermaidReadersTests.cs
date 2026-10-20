using FluentAssertions;
using Lld.Content.Authoring.Uml;

namespace Lld.Content.Tests.Authoring;

/// <summary>Spec 008 §3.4: reading class, state and sequence diagrams for the Code ↔ UML check.</summary>
public sealed class MermaidReadersTests
{
    [Theory]
    [InlineData("A <|-- B", "B", "A", RelationshipKind.Inheritance, true)]
    [InlineData("B --|> A", "B", "A", RelationshipKind.Inheritance, true)]
    [InlineData("IA <|.. B", "B", "IA", RelationshipKind.Realization, true)]
    [InlineData("B ..|> IA", "B", "IA", RelationshipKind.Realization, true)]
    [InlineData("A *-- B", "A", "B", RelationshipKind.Composition, true)]
    [InlineData("B --* A", "A", "B", RelationshipKind.Composition, true)]
    [InlineData("A o-- B", "A", "B", RelationshipKind.Aggregation, true)]
    [InlineData("B --o A", "A", "B", RelationshipKind.Aggregation, true)]
    [InlineData("A --> B", "A", "B", RelationshipKind.Association, true)]
    [InlineData("B <-- A", "A", "B", RelationshipKind.Association, true)]
    [InlineData("A ..> B", "A", "B", RelationshipKind.Dependency, true)]
    [InlineData("B <.. A", "A", "B", RelationshipKind.Dependency, true)]
    [InlineData("A -- B", "A", "B", RelationshipKind.Association, false)]
    [InlineData("A .. B", "A", "B", RelationshipKind.Dependency, false)]
    public void ClassDiagram_ReadsEveryArrow(string arrow, string source, string target, RelationshipKind kind, bool directed)
    {
        var model = MermaidClassDiagram.Parse($"classDiagram\n  {arrow}");

        var relationship = model.Relationships.Should().ContainSingle().Subject;
        (relationship.Source, relationship.Target, relationship.Kind, relationship.Directed, relationship.Line)
            .Should().Be((source, target, kind, directed, 2));
        relationship.Key.Should().Be(arrow.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public void ClassDiagram_ReadsCardinalitiesLabelsAndGenerics()
    {
        var model = MermaidClassDiagram.Parse("""
            classDiagram
              Order "1" *-- "1..*" OrderLine : has
              Repository~T~ ..> Order
            """);

        model.Relationships.Select(r => (r.Source, r.Target, r.Key)).Should().Equal(
            ("Order", "OrderLine", "Order*--OrderLine"),
            ("Repository", "Order", "Repository..>Order"));
        model.Classes.Select(c => c.Name).Should().Equal("Order", "OrderLine", "Repository");
    }

    [Fact]
    public void ClassDiagram_ReadsDeclarationsAndSkipsMembers()
    {
        var model = MermaidClassDiagram.Parse("""
            classDiagram
              direction LR
              %% Order --> Ghost
              class Order {
                +List~OrderLine~ lines
                +Add(OrderLine line) void
              }
              class Box~T~
              <<interface>> IShape
              Customer : +Name string
              note for Order "the whole"
            """);

        model.Classes.Select(c => (c.Name, c.Line)).Should().Equal(("Order", 4), ("Box", 8), ("IShape", 9), ("Customer", 10));
        model.Relationships.Should().BeEmpty();
    }

    [Fact]
    public void StateDiagram_ReadsStatesAndTransitions()
    {
        var model = MermaidStateDiagram.Parse("""
            stateDiagram-v2
              [*] --> Pending
              state "Decided" as Done
              Pending --> Approved : approve
              Pending --> Rejected : reject
              Approved --> [*]
              Pending : waiting for an admin
            """);

        model.States.Select(s => s.Name).Should().Equal("Pending", "Done", "Approved", "Rejected");
        model.Transitions.Select(t => (t.From, t.To, t.Event)).Should().Equal(
            ("[*]", "Pending", null),
            ("Pending", "Approved", "approve"),
            ("Pending", "Rejected", "reject"),
            ("Approved", "[*]", null));
    }

    [Fact]
    public void SequenceDiagram_ReadsParticipantsAliasesActorsAndMessages()
    {
        var model = MermaidSequenceDiagram.Parse("""
            sequenceDiagram
              actor U as Learner
              participant SM as SessionManager
              U->>SM: Login
              SM-->>+Tokens: Issue
              Tokens--)SM: done
            """);

        model.Participants.Select(p => (p.Id, p.Name, p.IsActor)).Should().Equal(
            ("U", "Learner", true),
            ("SM", "SessionManager", false),
            ("Tokens", "Tokens", false));
    }

    [Theory]
    [InlineData("classDiagram\n A --> B", DiagramType.Class)]
    [InlineData("  stateDiagram-v2\n", DiagramType.State)]
    [InlineData("sequenceDiagram", DiagramType.Sequence)]
    [InlineData("flowchart TD", DiagramType.Other)]
    public void DiagramType_FromFirstWord(string source, DiagramType type) => DiagramTypes.Of(source).Should().Be(type);
}
