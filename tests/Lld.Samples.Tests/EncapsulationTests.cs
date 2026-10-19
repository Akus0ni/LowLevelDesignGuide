using Lld.Samples.OopSolid.Encapsulation.Language;
using AfterAccount = Lld.Samples.OopSolid.Encapsulation.After.BankAccount;
using AfterOrder = Lld.Samples.OopSolid.Encapsulation.After.Order;
using BeforeAccount = Lld.Samples.OopSolid.Encapsulation.Before.BankAccount;
using BeforeOrder = Lld.Samples.OopSolid.Encapsulation.Before.Order;
using BeforeOrderLine = Lld.Samples.OopSolid.Encapsulation.Before.OrderLine;
using BeforeStatus = Lld.Samples.OopSolid.Encapsulation.Before.OrderStatus;
using RefundHandler = Lld.Samples.OopSolid.Encapsulation.Before.RefundHandler;

namespace Lld.Samples.Tests;

/// <summary>What the Encapsulation lesson claims about each design.</summary>
public sealed class EncapsulationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Before_AnyCallerCanBreakTheAccount()
    {
        var account = new BeforeAccount { Owner = "Asha" };

        account.Balance = -500;
        RefundHandler.Refund(account, 20);

        account.Balance.Should().Be(-480);
        account.Transactions.Should().BeEmpty("the history no longer explains the balance");
    }

    [Fact]
    public void After_TheBalanceAlwaysMatchesTheHistory()
    {
        var account = new AfterAccount("Asha");

        account.Deposit(100, Now);
        account.Withdraw(30, Now);

        account.Balance.Should().Be(70);
        account.Transactions.Sum(t => t.Amount).Should().Be(account.Balance);
    }

    [Fact]
    public void After_RefusesToOverdraw_AndLeavesNothingHalfDone()
    {
        var account = new AfterAccount("Asha");
        account.Deposit(10, Now);

        var overdraw = () => account.Withdraw(50, Now);

        overdraw.Should().Throw<InvalidOperationException>();
        account.Balance.Should().Be(10);
        account.Transactions.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void After_RefusesAmountsThatAreNotPositive(decimal amount)
    {
        var account = new AfterAccount("Asha");

        account.Invoking(a => a.Deposit(amount, Now)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void After_TheHistoryCannotBeChangedFromOutside()
    {
        var account = new AfterAccount("Asha");
        account.Deposit(10, Now);

        account.Transactions.Should().NotBeAssignableTo<List<OopSolid.Encapsulation.After.Transaction>>();
    }

    [Fact]
    public void BeforeOrder_CanChangeAfterItWasSubmitted()
    {
        var order = new BeforeOrder { Status = BeforeStatus.Submitted };

        order.Lines.Add(new BeforeOrderLine("Pen", 2, 1.5m));

        order.Lines.Should().ContainSingle();
        order.Total.Should().Be(0, "nobody updated the stored total");
    }

    [Fact]
    public void AfterOrder_AcceptsLinesOnlyWhileADraft_AndWorksOutItsTotal()
    {
        var order = new AfterOrder();
        order.AddLine("Pen", 2, 1.5m);
        order.AddLine("Pad", 1, 4m);

        order.Submit();

        order.Total.Should().Be(7m);
        order.Invoking(o => o.AddLine("Ink", 1, 3m)).Should().Throw<InvalidOperationException>();
        order.Invoking(o => o.Submit()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AfterOrder_CannotBeSubmittedEmpty() =>
        new AfterOrder().Invoking(o => o.Submit()).Should().Throw<InvalidOperationException>();

    [Fact]
    public void LeakyTeam_ReadOnlyInterface_CanBeCastBack()
    {
        var team = new LeakyTeam();
        team.Join("Asha");

        ((List<string>)team.Members).Clear();

        team.Members.Should().BeEmpty();
    }

    [Fact]
    public void Team_ReadOnlyWrapper_CannotBeChanged()
    {
        var team = new Team();
        team.Join("Asha");

        team.Members.Should().NotBeAssignableTo<List<string>>();
        team.Invoking(t => ((ICollection<string>)t.Members).Add("Ben")).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LeakyPercentage_With_SkipsTheConstructorCheck()
    {
        var half = new LeakyPercentage(50);

        var broken = half with { Value = 150 };

        broken.Value.Should().Be(150);
    }

    [Fact]
    public void Percentage_With_GoesThroughTheCheck()
    {
        var half = new Percentage(50);

        var broken = () => half with { Value = 150 };

        broken.Should().Throw<ArgumentOutOfRangeException>();
    }
}
