using System.Reflection;
using FluentAssertions;
using Lld.Samples.Uml.Activity;
using Lld.Samples.Uml.ClassBasics;
using Lld.Samples.Uml.Multiplicity;
using Lld.Samples.Uml.Relationships;
using Lld.Samples.Uml.Sequence;
using Lld.Samples.Uml.State;

namespace Lld.Samples.Tests.Uml;

/// <summary>Spec 008 §3.3: each UML sample behaves the way its diagram claims.</summary>
public sealed class UmlSampleTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    [Fact]
    public void ClassBasics_TheBoxDescribesWhatTheClassDoes()
    {
        var account = new BankAccount("Ann");
        account.Deposit(10m);

        account.Balance().Should().Be(10m);
        new PlainStatement().Print(account).Should().Be("Ann: 10.00");
        account.Invoking(a => a.Deposit(0m)).Should().Throw<ArgumentOutOfRangeException>();
        new DebitCard(50m).CanPay(60m).Should().BeFalse();
        typeof(Card).IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void Composition_OnlyTheOrderMakesItsLines()
    {
        var order = new Order();
        order.Add(new Product("Pen", 2m), 3);

        order.Lines.Should().ContainSingle().Which.Subtotal.Should().Be(6m);
        typeof(OrderLine).GetConstructors().Should().BeEmpty("no public constructor: lines can't exist outside an order");
    }

    [Fact]
    public void Aggregation_OrdersLiveOnWithoutTheCustomer()
    {
        var order = new Order();
        order.Add(new Product("Pen", 2m), 1);
        var customer = new Customer("Ann");

        customer.Keep(order);
        customer.Forget(order);

        customer.Orders.Should().BeEmpty();
        order.Lines.Should().ContainSingle("the order still exists, with its lines");
    }

    [Fact]
    public void Dependency_TheTaxCalculatorIsUsedNotKept()
    {
        var order = new Order();
        order.Add(new Product("Pen", 10m), 1);

        order.Total(new TaxCalculator(0.2m)).Should().Be(12m);
        typeof(Order).GetFields(Fields).Should().NotContain(f => f.FieldType == typeof(TaxCalculator));
    }

    [Fact]
    public void InheritanceAndRealization()
    {
        var ebook = new DigitalProduct("Ebook", 5m, new Uri("https://example.com/ebook"));

        ebook.Should().BeAssignableTo<Product>().And.BeAssignableTo<IPriced>();
        ebook.Price.Should().Be(5m);
    }

    [Fact]
    public void Multiplicity_ABookHasAtLeastOneAuthorAndAtMostOneBorrower()
    {
        var act = () => new Book("Nobody's", []);
        act.Should().Throw<ArgumentException>();

        var book = new Book("Clean Code", [new Author("Robert C. Martin")]);
        book.Lend(new Member("Ann"));
        book.Invoking(b => b.Lend(new Member("Ben"))).Should().Throw<InvalidOperationException>();
        book.Return();
        book.Borrower.Should().BeNull();
    }

    [Fact]
    public void Navigability_AMemberDoesNotKnowItsBooks() =>
        typeof(Member).GetProperties().Should().NotContain(p => p.PropertyType == typeof(Book));

    [Fact]
    public void Sequence_ChargesThenSendsTheReceipt_InThatOrder()
    {
        var log = new List<string>();
        var service = new CheckoutService(new FakeGateway(log, approve: true), new FakeSender(log));
        var cart = new Cart();
        cart.Add(4m);

        var receipt = service.Checkout(cart, "4242");

        receipt.Should().Be(new Receipt("p1", 4m));
        log.Should().Equal("charge 4242 4", "send p1");
    }

    [Fact]
    public void Sequence_ADeclinedCardSendsNothing()
    {
        var log = new List<string>();
        var service = new CheckoutService(new FakeGateway(log, approve: false), new FakeSender(log));

        service.Checkout(new Cart(), "0000").Should().BeNull();
        log.Should().Equal("charge 0000 0");
    }

    public static TheoryData<ParcelStatus, string, ParcelStatus?> ParcelMoves => new()
    {
        // The diagram's arrows...
        { ParcelStatus.Created, "ship", ParcelStatus.Shipped },
        { ParcelStatus.Created, "cancel", ParcelStatus.Cancelled },
        { ParcelStatus.Shipped, "deliver", ParcelStatus.Delivered },
        // ...and everything it doesn't draw is refused.
        { ParcelStatus.Created, "deliver", null },
        { ParcelStatus.Shipped, "ship", null },
        { ParcelStatus.Shipped, "cancel", null },
        { ParcelStatus.Delivered, "ship", null },
        { ParcelStatus.Delivered, "cancel", null },
        { ParcelStatus.Delivered, "deliver", null },
        { ParcelStatus.Cancelled, "ship", null },
        { ParcelStatus.Cancelled, "deliver", null },
        { ParcelStatus.Cancelled, "cancel", null },
    };

    [Theory]
    [MemberData(nameof(ParcelMoves))]
    public void State_TheParcelAllowsExactlyTheDrawnTransitions(ParcelStatus from, string action, ParcelStatus? to)
    {
        var parcel = ParcelIn(from);

        var act = () => Do(parcel, action);

        if (to is { } expected)
        {
            act();
            parcel.Status.Should().Be(expected);
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
            parcel.Status.Should().Be(from);
        }
    }

    [Theory]
    [InlineData("0000", 10, WithdrawalOutcome.WrongPin, 100)]
    [InlineData("1234", 500, WithdrawalOutcome.NotEnoughMoney, 100)]
    [InlineData("1234", 40, WithdrawalOutcome.Dispensed, 60)]
    public void Activity_TakesTheBranchTheDiagramShows(string pin, decimal amount, WithdrawalOutcome outcome, decimal balance)
    {
        var machine = new CashMachine("1234", 100m);

        machine.Withdraw(pin, amount).Should().Be(outcome);

        machine.Balance.Should().Be(balance);
        machine.Done.Should().Equal(outcome == WithdrawalOutcome.Dispensed ? ["dispense cash", "print receipt"] : []);
    }

    private static Parcel ParcelIn(ParcelStatus status)
    {
        var parcel = new Parcel();
        switch (status)
        {
            case ParcelStatus.Shipped:
                parcel.Ship();
                break;
            case ParcelStatus.Delivered:
                parcel.Ship();
                parcel.Deliver();
                break;
            case ParcelStatus.Cancelled:
                parcel.Cancel();
                break;
        }

        return parcel;
    }

    private static void Do(Parcel parcel, string action)
    {
        switch (action)
        {
            case "ship":
                parcel.Ship();
                break;
            case "cancel":
                parcel.Cancel();
                break;
            default:
                parcel.Deliver();
                break;
        }
    }

    private sealed class FakeGateway(List<string> log, bool approve) : IPaymentGateway
    {
        public Payment Charge(string card, decimal amount)
        {
            log.Add($"charge {card} {amount}");
            return new Payment("p1", approve);
        }
    }

    private sealed class FakeSender(List<string> log) : IReceiptSender
    {
        public void Send(Receipt receipt) => log.Add($"send {receipt.PaymentId}");
    }
}
