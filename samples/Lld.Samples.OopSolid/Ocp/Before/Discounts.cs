namespace Lld.Samples.OopSolid.Ocp.Before;

public sealed record Cart(decimal Total, string CustomerType, bool IsFestivalWeek);

/// <summary>Every new discount means opening this method again, and retesting every discount already in it.</summary>
public static class DiscountCalculator
{
    public static decimal Discount(Cart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        var discount = cart.CustomerType switch
        {
            "gold" => cart.Total * 0.10m,
            "silver" => cart.Total * 0.05m,
            _ => 0m,
        };

        // Added in March. Then "student" in June. Then...
        if (cart.IsFestivalWeek)
        {
            discount += 100m;
        }

        return Math.Min(discount, cart.Total);
    }
}
