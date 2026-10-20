namespace Lld.Samples.Patterns.Behavioral.Strategy.Before;

/// <summary>
/// Every way of shipping lives in one <c>switch</c>. Adding same-day delivery means opening and retesting checkout,
/// and the same <c>switch</c> is copied into the cart preview and the invoice.
/// </summary>
public static class Checkout
{
    public static decimal Total(Order order, string shipping) => order.Subtotal + shipping switch
    {
        "standard" => order.WeightKg * 20m,
        "express" => 100m + order.WeightKg * 40m,
        "pickup" => 0m,
        _ => throw new ArgumentException($"Unknown shipping '{shipping}'.", nameof(shipping)),
    };
}
