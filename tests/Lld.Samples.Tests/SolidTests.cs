using Dip = Lld.Samples.OopSolid.Dip;
using Isp = Lld.Samples.OopSolid.Isp;
using Lsp = Lld.Samples.OopSolid.Lsp;
using Ocp = Lld.Samples.OopSolid.Ocp;
using Srp = Lld.Samples.OopSolid.Srp;

namespace Lld.Samples.Tests;

/// <summary>What the SOLID lessons (batch 3) claim about each design.</summary>
public sealed class SolidTests
{
    [Fact]
    public void Srp_SplitClasses_ProduceTheSameInvoice()
    {
        var before = new Srp.Before.Invoice("INV-7", [new("Pen", 2, 50m)]);
        var after = new Srp.After.Invoice("INV-7", [new("Pen", 2, 50m)]);

        after.Total.Should().Be(before.Total()).And.Be(118m);
        Srp.After.InvoiceHtml.Render(after).Should().Be(before.ToHtml());
    }

    [Fact]
    public void Ocp_Before_TheSameMethodHoldsEveryDiscount() =>
        Ocp.Before.DiscountCalculator.Discount(new Ocp.Before.Cart(1000, "gold", IsFestivalWeek: true)).Should().Be(200m);

    [Fact]
    public void Ocp_After_ANewRuleIsANewClass_AndTheCalculatorIsUntouched()
    {
        var cart = new Ocp.After.Cart(1000, "gold", IsFestivalWeek: true);
        Ocp.After.IDiscountRule[] rules = [new Ocp.After.LoyaltyDiscount(), new Ocp.After.FestivalDiscount()];

        new Ocp.After.DiscountCalculator(rules).Discount(cart).Should().Be(200m);
        new Ocp.After.DiscountCalculator([.. rules, new Ocp.After.FirstOrderDiscount(isFirstOrder: true)]).Discount(cart).Should().Be(250m);
    }

    [Fact]
    public void Ocp_After_NeverDiscountsMoreThanTheTotal() =>
        new Ocp.After.DiscountCalculator([new Ocp.After.FestivalDiscount()]).Discount(new Ocp.After.Cart(40, "none", true)).Should().Be(40m);

    [Fact]
    public void Lsp_Before_ASquareBreaksCodeWrittenForRectangles()
    {
        Lsp.Before.Resizer.StretchToTenByFive(new Lsp.Before.Rectangle()).Should().Be(50);
        Lsp.Before.Resizer.StretchToTenByFive(new Lsp.Before.Square()).Should().Be(25, "setting the height also changed the width");
    }

    [Fact]
    public void Lsp_After_EveryShapeKeepsThePromiseItMakes() =>
        Lsp.After.Shapes.TotalArea([new Lsp.After.Rectangle(10, 5), new Lsp.After.Square(5)]).Should().Be(75);

    [Fact]
    public void Isp_Before_AFatInterfaceFailsAtRuntime()
    {
        Isp.Before.IOfficeMachine machine = new Isp.Before.BasicPrinter();

        machine.Invoking(m => m.Scan("page 1")).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Isp_After_ClientsAskForTheRoleTheyNeed()
    {
        Isp.After.Reception.PrintVisitorBadge(new Isp.After.BasicPrinter(), "Asha").Should().Be("printed badge for Asha");
        Isp.After.Reception.PrintVisitorBadge(new Isp.After.AllInOne(), "Ben").Should().Be("printed badge for Ben");
        new Isp.After.BasicPrinter().Should().NotBeAssignableTo<Isp.After.IScanner>();
    }

    [Fact]
    public void Dip_Before_CheckoutCannotRunWithoutAMailServer() =>
        new Dip.Before.CheckoutService().Invoking(s => s.Checkout("asha@example.com", 99)).Should().Throw<InvalidOperationException>();

    [Fact]
    public void Dip_After_CheckoutRunsWithAnyNotifier()
    {
        var recording = new Dip.After.RecordingNotifier();
        var mailed = new List<string>();

        new Dip.After.CheckoutService(recording).Checkout("asha@example.com", 99).Should().Be(99);
        new Dip.After.CheckoutService(new Dip.After.EmailNotifier((to, _) => mailed.Add(to))).Checkout("ben@example.com", 5);

        recording.Sent.Should().Equal("asha@example.com: Thanks! You paid 99.");
        mailed.Should().Equal("ben@example.com");
    }
}
