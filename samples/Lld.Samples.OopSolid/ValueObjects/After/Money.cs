namespace Lld.Samples.OopSolid.ValueObjects.After;

/// <summary>
/// A value object: defined by its values, equal when the values are equal, never changed (an operation returns a new
/// value). It can't be created invalid, so code that receives one can trust it.
/// </summary>
public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money operator +(Money left, Money right) =>
        left.Currency == right.Currency
            ? new Money(left.Amount + right.Amount, left.Currency)
            : throw new InvalidOperationException($"Can't add {right.Currency} to {left.Currency}.");

    public override string ToString() => $"{Amount} {Currency}";
}

/// <summary>An account number that is checked once, here, and then trusted everywhere.</summary>
public sealed record AccountNumber
{
    private const int Length = 10;

    public AccountNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != Length || !value.All(char.IsAsciiDigit))
        {
            throw new ArgumentException($"An account number is {Length} digits.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>The types now say what each argument is: swapping them doesn't compile.</summary>
public static class Transfers
{
    public static string Describe(AccountNumber from, AccountNumber to, Money amount) => $"{amount} from {from} to {to}";
}
