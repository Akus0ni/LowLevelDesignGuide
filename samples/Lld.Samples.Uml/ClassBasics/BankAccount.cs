namespace Lld.Samples.Uml.ClassBasics;

/// <summary>
/// One class box: the name on top, fields in the middle, methods at the bottom. The marks say who can see each
/// member: + public, - private, # protected, ~ internal; a static member is underlined.
/// </summary>
public class BankAccount(string owner)
{
    private decimal _balance;

    public string Owner { get; } = owner;

    protected DateTimeOffset OpenedOn { get; } = DateTimeOffset.UtcNow;

    internal string Branch { get; init; } = "Main";

    public static int InterestRatePercent => 2;

    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        _balance += amount;
    }

    public decimal Balance() => _balance;
}

/// <summary>«interface»: only what it can do, no data.</summary>
public interface IStatement
{
    string Print(BankAccount account);
}

/// <summary>«abstract»: some methods have no body (drawn in italics, or marked with *).</summary>
public abstract class Card
{
    public abstract decimal Limit();

    public bool CanPay(decimal amount) => amount <= Limit();
}

public sealed class DebitCard(decimal limit) : Card
{
    public override decimal Limit() => limit;
}

public sealed class PlainStatement : IStatement
{
    public string Print(BankAccount account) => $"{account.Owner}: {account.Balance():0.00}";
}
