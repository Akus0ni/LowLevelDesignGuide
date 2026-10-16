using Microsoft.Extensions.Time.Testing;
using Lld.Samples.OopSolid.CompositionOverInheritance.After;
using Lld.Samples.OopSolid.InterfacesVsAbstractClasses.After;
using Lld.Samples.OopSolid.InterfacesVsAbstractClasses.Language;
using Lld.Samples.OopSolid.ValueObjects.After;
using BeforeLoggers = Lld.Samples.OopSolid.CompositionOverInheritance.Before;
using BeforePayments = Lld.Samples.OopSolid.InterfacesVsAbstractClasses.Before;
using Coupling = Lld.Samples.OopSolid.CouplingCohesion;
using Demeter = Lld.Samples.OopSolid.LawOfDemeter;

namespace Lld.Samples.Tests;

/// <summary>What the batch 2 lessons (design basics) claim about each design.</summary>
public sealed class DesignBasicsTests
{
    [Fact]
    public void AbstractClassAsContract_ForcesAWalletToFakeACardNumber() =>
        new BeforePayments.UpiWallet("asha@upi").Masked.Should().Be("**** 0000");

    [Fact]
    public void InterfaceAsContract_LetsVeryDifferentThingsPay()
    {
        IPaymentMethod[] methods = [new CreditCard("4111111111111111"), new DebitCard("5500000000000004", balance: 100), new UpiWallet("asha@upi")];

        methods.Select(m => m.Label).Should().Equal("**** 1111", "**** 0004", "asha@upi");
        methods.Select(m => m.Pay(500)).Should().Equal(true, false, true);
    }

    [Fact]
    public void DefaultInterfaceMethod_GivesOldImplementationsTheNewMember() =>
        ((IGreeter)new PoliteGreeter()).GreetAll(["Asha", "Ben"]).Should().Be("Hello, Asha. Hello, Ben.");

    [Fact]
    public void StaticAbstractMembers_LetGenericCodeAskTheType() =>
        Totals.Sum([new Points(2), new Points(3)]).Should().Be(new Points(5));

    [Fact]
    public void Inheritance_NeedsAClassPerCombination()
    {
        var logger = new BeforeLoggers.TimestampedUpperCaseLogger();

        logger.Log("saved");

        logger.Written.Should().Equal("[09:00] SAVED");
    }

    [Fact]
    public void Composition_CombinesBricksAtRuntime_InAnyOrder()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        var memory = new MemoryLog();

        new Timestamped(new UpperCase(memory), clock).Log("saved");
        new UpperCase(new Timestamped(memory, clock)).Log("loaded");

        memory.Written.Should().Equal("[09:00] SAVED", "[09:00] LOADED");
    }

    [Fact]
    public void PrimitiveObsession_HappilyAddsRupeesToDollars() =>
        Lld.Samples.OopSolid.ValueObjects.Before.Transfers.Total(100, "INR", 5, "USD").Should().Be(105);

    [Fact]
    public void Money_IsEqualByValue_AndRefusesToMixCurrencies()
    {
        (new Money(100, "inr") == new Money(100, "INR")).Should().BeTrue();
        (new Money(100, "INR") + new Money(50, "INR")).Should().Be(new Money(150, "INR"));

        var mix = () => new Money(100, "INR") + new Money(5, "USD");

        mix.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345abcde")]
    public void AccountNumber_CannotBeCreatedInvalid(string value) =>
        FluentActions.Invoking(() => new AccountNumber(value)).Should().Throw<ArgumentException>();

    [Fact]
    public void Transfers_SayWhatEachArgumentIs() =>
        Lld.Samples.OopSolid.ValueObjects.After.Transfers.Describe(new AccountNumber("1111111111"), new AccountNumber("2222222222"), new Money(10, "INR"))
            .Should().Be("10 INR from 1111111111 to 2222222222");

    [Fact]
    public void CohesiveReceipt_PrintsTheSameFromASmallerDependency()
    {
        var placed = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        var before = Coupling.Before.OrderHelpers.Receipt(new Coupling.Before.Order(
            new Coupling.Before.Customer("Asha", "asha@example.com", "Pune"), [new("Pen", 2, 1.5m)], placed));
        var after = Coupling.After.ReceiptFormatter.Format(Coupling.After.ReceiptData.From(
            new Coupling.After.Order("Asha", [new("Pen", 2, 1.5m)], placed)));

        after.Should().Be("Asha, 2026-10-16: Pen x2 = 3.0");
        before.Should().Be("Asha (Pune), 2026-10-16: Pen x2 = 3.0");
    }

    [Fact]
    public void Paperboy_ReachingIntoTheWallet_CanTakeMoneyThatIsNotThere()
    {
        var wallet = new Demeter.Before.Wallet { Cash = 1 };

        new Demeter.Before.Paperboy().Collect(new Demeter.Before.Customer("Asha", wallet), 2);

        wallet.Cash.Should().Be(-1);
    }

    [Fact]
    public void Paperboy_AskingTheCustomer_LetsTheCustomerDecide()
    {
        var paperboy = new Demeter.After.Paperboy();

        paperboy.Collect(new Demeter.After.Customer("Asha", cash: 1), 2).Should().BeFalse();
        paperboy.Collect(new Demeter.After.Customer("Ben", cash: 5), 2).Should().BeTrue();
        paperboy.Collected.Should().Be(2);
    }
}
