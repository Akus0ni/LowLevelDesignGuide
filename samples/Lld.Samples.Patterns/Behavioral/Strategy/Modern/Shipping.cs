namespace Lld.Samples.Patterns.Behavioral.Strategy.Modern;

/// <summary>
/// Modern C#: a strategy with one method is just a function. Name them in one place and pass a
/// <see cref="Func{T, TResult}"/>; reach for an interface when a strategy needs dependencies or several methods.
/// </summary>
public static class ShippingCosts
{
    public static readonly Func<Order, decimal> Standard = order => order.WeightKg * 20m;
    public static readonly Func<Order, decimal> Express = order => 100m + order.WeightKg * 40m;
    public static readonly Func<Order, decimal> Pickup = _ => 0m;
}

public sealed class Checkout(Func<Order, decimal> shippingCost)
{
    public decimal Total(Order order) => order.Subtotal + shippingCost(order);
}
