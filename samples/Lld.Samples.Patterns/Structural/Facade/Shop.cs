namespace Lld.Samples.Patterns.Structural.Facade;

/// <summary>The subsystem: four classes that each do one job well. Shared by every version.</summary>
public sealed class Inventory
{
    private readonly Dictionary<string, int> _stock = new(StringComparer.Ordinal) { ["pen"] = 5 };

    public bool Reserve(string sku)
    {
        if (_stock.GetValueOrDefault(sku) == 0)
        {
            return false;
        }

        _stock[sku]--;
        return true;
    }

    public void Release(string sku) => _stock[sku] = _stock.GetValueOrDefault(sku) + 1;

    public int Left(string sku) => _stock.GetValueOrDefault(sku);
}

public sealed class Payments
{
    public List<decimal> Charged { get; } = [];

    /// <summary>Cards starting with "ok" are accepted; returns a receipt, or null when declined.</summary>
    public string? Charge(string card, decimal amount)
    {
        if (!card.StartsWith("ok", StringComparison.Ordinal))
        {
            return null;
        }

        Charged.Add(amount);
        return $"pay-{Charged.Count}";
    }
}

public sealed class Shipping
{
    public List<string> Booked { get; } = [];

    public string Book(string address)
    {
        Booked.Add(address);
        return $"ship-{Booked.Count}";
    }
}

public sealed class Mailer
{
    public List<string> Sent { get; } = [];

    public void Send(string text) => Sent.Add(text);
}

public sealed record OrderResult(bool Placed, string? Tracking);
