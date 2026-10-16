using System.Globalization;

namespace Lld.Samples.OopSolid.CouplingCohesion.After;

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;
}

/// <summary>Pricing lives with the order lines it prices (high cohesion).</summary>
public sealed record Order(string CustomerName, IReadOnlyList<OrderLine> Lines, DateTimeOffset PlacedAt)
{
    public decimal Total => Lines.Sum(line => line.Amount);
}

/// <summary>
/// The receipt needs only what it prints, so it depends on one small record (low coupling). The customer's address, the
/// e-mail rules and how totals are worked out can all change without touching it.
/// </summary>
public sealed record ReceiptData(string CustomerName, DateOnly Date, IReadOnlyList<string> Items, decimal Total)
{
    public static ReceiptData From(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new(order.CustomerName, DateOnly.FromDateTime(order.PlacedAt.Date),
            [.. order.Lines.Select(line => $"{line.Product} x{line.Quantity}")], order.Total);
    }
}

public static class ReceiptFormatter
{
    public static string Format(ReceiptData receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return $"{receipt.CustomerName}, {receipt.Date:yyyy-MM-dd}: {string.Join(", ", receipt.Items)} = {receipt.Total.ToString(CultureInfo.InvariantCulture)}";
    }
}
