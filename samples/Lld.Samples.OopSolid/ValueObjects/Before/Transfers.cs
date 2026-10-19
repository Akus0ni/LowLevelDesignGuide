namespace Lld.Samples.OopSolid.ValueObjects.Before;

/// <summary>
/// Primitive obsession: an account number, an amount and a currency are all "just" strings and decimals. Nothing stops
/// swapped arguments, a negative amount or adding rupees to dollars.
/// </summary>
public static class Transfers
{
    public static string Describe(string fromAccount, string toAccount, decimal amount, string currency) =>
        $"{amount} {currency} from {fromAccount} to {toAccount}";

    public static decimal Total(decimal first, string firstCurrency, decimal second, string secondCurrency)
    {
        ArgumentNullException.ThrowIfNull(firstCurrency);
        ArgumentNullException.ThrowIfNull(secondCurrency);
        return first + second;
    }
}
