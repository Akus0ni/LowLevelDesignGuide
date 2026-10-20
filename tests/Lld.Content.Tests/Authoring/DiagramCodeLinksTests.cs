using FluentAssertions;
using Lld.Content.Authoring.Uml;

namespace Lld.Content.Tests.Authoring;

/// <summary>Spec 008 §3.4: reading the paired code and matching a diagram against it.</summary>
public sealed class DiagramCodeLinksTests
{
    private const string Code = """
        public sealed class Order(Customer customer) : IEntity
        {
            private readonly List<OrderLine> _lines = [];

            public Customer? Customer { get; } = customer;

            public Discount[] Discounts { get; } = [];

            public void Add(OrderLine line) => _lines.Add(line);

            public decimal Total(TaxPolicy tax) => tax.Apply(0m);
        }

        public sealed record OrderLine(string Sku, Product Product);

        public interface IEntity;

        public sealed class Customer;

        public sealed class Product;

        public sealed class Discount;

        public sealed class TaxPolicy
        {
            public decimal Apply(decimal amount) => amount;
        }

        public enum OrderStatus
        {
            Open,
            Paid,
        }
        """;

    [Fact]
    public void CodeModel_ReadsTypesBasesMembersAndEnumMembers()
    {
        var model = SnippetCodeModel.Of(Code);

        var order = model.Type("Order")!;
        order.Lines.Should().Be(new CodeLines(1, 12));
        order.Bases.Should().Equal(new CodeUse("IEntity", 1));
        order.Members.Should().BeEquivalentTo(
            [new CodeMember(["OrderLine"], true, 3), new CodeMember(["Customer"], false, 5), new CodeMember(["Discount"], true, 7)],
            o => o.WithStrictOrdering());
        model.Type("OrderLine")!.Members.Select(m => (string.Join(',', m.TypeNames), m.Line)).Should().Equal((string.Empty, 14), ("Product", 14));
        model.EnumMember("Paid").Should().Be(32);
        model.Type("Ghost").Should().BeNull();
    }

    [Fact]
    public void ClassDiagram_LinksClassesAndEveryKindOfArrow()
    {
        var result = Links("""
            classDiagram
              IEntity <|.. Order
              Order *-- OrderLine
              Order --> Customer
              Order o-- Discount
              Order ..> TaxPolicy
              OrderLine -- Product
            """);

        result.Mismatches.Should().BeEmpty();
        result.Links.Should().ContainEquivalentOf(new DiagramLink("Order", [.. Enumerable.Range(1, 12)]));
        result.Links.Where(l => l.Key.Contains('-') || l.Key.Contains('.')).Select(l => (l.Key, string.Join(',', l.Lines))).Should().Equal(
            ("IEntity<|..Order", "1"),
            ("Order*--OrderLine", "3"),
            ("Order-->Customer", "5"),
            ("Ordero--Discount", "7"),
            ("Order..>TaxPolicy", "11"),
            ("OrderLine--Product", "14"));
    }

    [Fact]
    public void LinksFormat_AsThePackedAttribute() =>
        Links("classDiagram\n  Order --> Customer").Format().Should().Be("Order=1-12;Customer=18;Order-->Customer=5");

    [Theory]
    [InlineData("Order --> Ghost", 2, "'Ghost' is in the diagram but no type of that name is in the code.")]
    [InlineData("Customer <|-- Order", 2, "'Customer<|--Order': Order doesn't list Customer as a base type.")]
    [InlineData("Order --> Product", 2, "'Order-->Product': Order has no field or property of type Product.")]
    [InlineData("Customer --> Order", 2, "'Customer-->Order': Customer has no field or property of type Order.")]
    [InlineData("Customer -- Product", 2, "'Customer--Product': neither Customer nor Product has a field or property of the other's type.")]
    [InlineData("Customer ..> TaxPolicy", 2, "'Customer..>TaxPolicy': Customer never uses TaxPolicy.")]
    public void ClassDiagram_Mismatches(string arrow, int line, string message) =>
        Links($"classDiagram\n  {arrow}").Mismatches.Should().Equal(new DiagramMismatch(line, message));

    [Fact]
    public void StateDiagram_StatesAreEnumMembersOrTypes()
    {
        var result = Links("""
            stateDiagram-v2
              [*] --> Open
              Open --> Paid : pay
              Paid --> Refunded : refund
            """);

        result.Links.Should().BeEquivalentTo([new DiagramLink("Open", [31]), new DiagramLink("Paid", [32])], o => o.WithStrictOrdering());
        result.Mismatches.Should().Equal(new DiagramMismatch(4, "State 'Refunded' is not an enum member or a type in the code."));
    }

    [Fact]
    public void SequenceDiagram_ParticipantsAreTypes_ActorsAreSkipped()
    {
        var result = Links("""
            sequenceDiagram
              actor U as Learner
              participant O as Order
              U->>O: Add
              O->>Ghost: Save
            """);

        result.Links.Select(l => l.Key).Should().Equal("O");
        result.Mismatches.Should().Equal(new DiagramMismatch(5, "Participant 'Ghost' is not a type in the code (declare people with 'actor')."));
    }

    [Fact]
    public void OtherDiagrams_HaveNoLinks() =>
        Links("flowchart TD\n  a --> b").Should().BeEquivalentTo(new DiagramCodeResult([], []));

    private static DiagramCodeResult Links(string diagram) => DiagramCodeLinks.For(diagram, SnippetCodeModel.Of(Code));
}
