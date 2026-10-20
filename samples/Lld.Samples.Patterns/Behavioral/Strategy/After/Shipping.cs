namespace Lld.Samples.Patterns.Behavioral.Strategy.After;

/// <summary>The strategy: one way of pricing shipping.</summary>
public interface IShippingCost
{
    decimal For(Order order);
}

public sealed class StandardShipping : IShippingCost
{
    public decimal For(Order order) => order.WeightKg * 20m;
}

public sealed class ExpressShipping : IShippingCost
{
    public decimal For(Order order) => 100m + order.WeightKg * 40m;
}

public sealed class StorePickup : IShippingCost
{
    public decimal For(Order order) => 0m;
}

/// <summary>The context: uses whichever strategy it is given and never asks which one it is.</summary>
public sealed class Checkout(IShippingCost shipping)
{
    public decimal Total(Order order) => order.Subtotal + shipping.For(order);
}
