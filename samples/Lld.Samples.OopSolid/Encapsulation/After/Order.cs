namespace Lld.Samples.OopSolid.Encapsulation.After;

public enum OrderStatus
{
    Draft,
    Submitted,
}

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;
}

/// <summary>
/// An order changes only through what it allows in its current status: lines are added while it is a draft, and it
/// can be submitted once, with at least one line. The total is worked out, never stored, so it can't go stale.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(line => line.Amount);

    public void AddLine(string product, int quantity, decimal unitPrice)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        _lines.Add(new OrderLine(product, quantity, unitPrice));
    }

    public void Submit()
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("An order needs at least one line before it is submitted.");
        }

        Status = OrderStatus.Submitted;
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"The order is {Status}; it can't change any more.");
        }
    }
}
