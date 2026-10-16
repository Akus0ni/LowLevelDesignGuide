namespace Lld.Samples.OopSolid.Polymorphism.After;

/// <summary>Everything checkout needs to know about a way of shipping. Each carrier answers for itself.</summary>
public interface IShippingMethod
{
    string Label { get; }

    int DaysToDeliver { get; }

    decimal Cost(decimal weightKg);
}

public sealed class Post : IShippingMethod
{
    public string Label => "Standard post";

    public int DaysToDeliver => 3;

    public decimal Cost(decimal weightKg) => 3.50m + (0.80m * weightKg);
}

public sealed class Courier : IShippingMethod
{
    public string Label => "Next-day courier";

    public int DaysToDeliver => 1;

    public decimal Cost(decimal weightKg) => 9.00m + (1.20m * weightKg);
}

public sealed class Pickup : IShippingMethod
{
    public string Label => "Collect in store";

    public int DaysToDeliver => 0;

    public decimal Cost(decimal weightKg) => 0m;
}

/// <summary>Works with any shipping method, including ones written after it.</summary>
public static class Checkout
{
    public static IShippingMethod Cheapest(IEnumerable<IShippingMethod> methods, decimal weightKg) =>
        methods.MinBy(method => method.Cost(weightKg))
        ?? throw new ArgumentException("Offer at least one shipping method.", nameof(methods));

    public static IShippingMethod Fastest(IEnumerable<IShippingMethod> methods) =>
        methods.MinBy(method => method.DaysToDeliver)
        ?? throw new ArgumentException("Offer at least one shipping method.", nameof(methods));
}

/// <summary>Added a year later: a new class, and not one line of <see cref="Checkout"/> changed.</summary>
public sealed class Drone : IShippingMethod
{
    public string Label => "Drone (same day)";

    public int DaysToDeliver => 0;

    public decimal Cost(decimal weightKg) => 12.00m + (2.00m * weightKg);
}
