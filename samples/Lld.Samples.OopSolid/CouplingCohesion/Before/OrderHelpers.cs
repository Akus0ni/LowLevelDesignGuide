using System.Globalization;

namespace Lld.Samples.OopSolid.CouplingCohesion.Before;

public sealed record Customer(string Name, string Email, string City);

public sealed record OrderLine(string Product, int Quantity, decimal UnitPrice);

public sealed record Order(Customer Customer, IReadOnlyList<OrderLine> Lines, DateTimeOffset PlacedAt);

/// <summary>
/// The junk drawer: pricing, receipts and e-mail rules in one class (low cohesion). The receipt reads deep into the
/// order, its customer and their city (high coupling): change any of them and this breaks.
/// </summary>
public static class OrderHelpers
{
    public static decimal Total(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.Lines.Sum(line => line.Quantity * line.UnitPrice);
    }

    public static string Receipt(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        var lines = order.Lines.Select(line => $"{line.Product} x{line.Quantity}");
        return $"{order.Customer.Name} ({order.Customer.City}), {order.PlacedAt:yyyy-MM-dd}: {string.Join(", ", lines)} = {Total(order).ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool IsValidEmail(string email) => email?.Contains('@', StringComparison.Ordinal) == true;
}
