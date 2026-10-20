namespace Lld.Samples.Patterns.Structural.Proxy;

/// <summary>The subject: what callers want, a rate for a currency.</summary>
public interface IExchangeRates
{
    decimal RateFor(string currency);
}

/// <summary>The real subject: a slow call to a remote service (here, it only counts the calls). Shared by every version.</summary>
public sealed class RemoteRates : IExchangeRates
{
    public int Calls { get; private set; }

    public decimal RateFor(string currency)
    {
        Calls++;
        return currency == "USD" ? 83.2m : 1m;
    }
}
