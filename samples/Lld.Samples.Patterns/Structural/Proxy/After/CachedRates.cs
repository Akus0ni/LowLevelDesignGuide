namespace Lld.Samples.Patterns.Structural.Proxy.After;

/// <summary>
/// The proxy: the same interface as the real service, standing in front of it. It answers from memory when it can and
/// calls the real one only the first time. Callers can't tell the difference.
/// </summary>
public sealed class CachedRates(IExchangeRates real) : IExchangeRates
{
    private readonly Dictionary<string, decimal> _known = new(StringComparer.Ordinal);

    public decimal RateFor(string currency)
    {
        if (!_known.TryGetValue(currency, out var rate))
        {
            rate = real.RateFor(currency);
            _known[currency] = rate;
        }

        return rate;
    }
}

/// <summary>The client is unchanged: it asks an <see cref="IExchangeRates"/> and is handed the proxy.</summary>
public sealed class PriceList(IExchangeRates rates)
{
    public IReadOnlyList<decimal> InCurrency(IEnumerable<decimal> pricesInUsd, string currency) =>
        [.. pricesInUsd.Select(price => price * rates.RateFor("USD") / rates.RateFor(currency))];
}
