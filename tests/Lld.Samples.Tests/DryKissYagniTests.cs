using After = Lld.Samples.OopSolid.DryKissYagni.After;
using Before = Lld.Samples.OopSolid.DryKissYagni.Before;

namespace Lld.Samples.Tests;

/// <summary>What the DRY, KISS & YAGNI lesson claims about each design.</summary>
public sealed class DryKissYagniTests
{
    [Fact]
    public void Dry_Before_ThreeCopiesOfOneRule_AlreadyDisagree()
    {
        Before.Cart.Shipping(500).Should().Be(0, "the cart says shipping is free at exactly 500");
        Before.OrderEmail.ShippingLine(500).Should().Be("Shipping: ₹49", "the e-mail copy used > instead of >=");
    }

    [Theory]
    [InlineData(499, 49, "Shipping: ₹49")]
    [InlineData(500, 0, "Free shipping!")]
    public void Dry_After_EveryPlaceAsksTheOneRule(decimal subtotal, decimal cost, string line)
    {
        After.Cart.Shipping(subtotal).Should().Be(cost);
        After.Invoice.Shipping(subtotal).Should().Be(cost);
        After.OrderEmail.ShippingLine(subtotal).Should().Be(line);
    }

    [Fact]
    public void Kiss_OneMethodDoesWhatTheEngineDid()
    {
        var engine = new Before.DiscountEngine().Register("member", total => total * 0.9m);

        After.MemberDiscount.Apply(200, isMember: true).Should().Be(engine.Apply("member", 200)).And.Be(180);
        After.MemberDiscount.Apply(200, isMember: false).Should().Be(200);
    }

    [Fact]
    public void Yagni_Before_SpeculativeCodeOnlyThrows() =>
        new Before.PdfExporter().Invoking(e => e.Export(["a"])).Should().Throw<NotImplementedException>();

    [Fact]
    public void Yagni_After_BuildsWhatWasAskedFor() =>
        After.CsvReport.Export(["a,1", "b,2"]).Should().Be("a,1\nb,2");
}
