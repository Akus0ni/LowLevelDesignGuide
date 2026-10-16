namespace Lld.Samples.OopSolid.Encapsulation.Before;

public enum OrderStatus
{
    Draft,
    Submitted,
}

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice);

/// <summary>Getters and setters for everything, and a list anyone can change at any time.</summary>
public sealed class Order
{
    public OrderStatus Status { get; set; }

    public List<OrderLine> Lines { get; } = [];

    public decimal Total { get; set; }
}
