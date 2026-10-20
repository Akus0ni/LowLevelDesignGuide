namespace Lld.Samples.Patterns.Structural.Proxy.Before;

/// <summary>
/// The price list asks the remote service for every single price, even though the rate is the same for all of them.
/// Adding a cache here mixes caching into pricing, and every other caller needs its own copy.
/// </summary>
public sealed class PriceList(RemoteRates rates)
{
    public IReadOnlyList<decimal> InCurrency(IEnumerable<decimal> pricesInUsd, string currency) =>
        [.. pricesInUsd.Select(price => price * rates.RateFor("USD") / rates.RateFor(currency))];
}
