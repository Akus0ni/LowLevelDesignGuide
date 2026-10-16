namespace Lld.Samples.OopSolid.Ocp.After;

public sealed record Cart(decimal Total, string CustomerType, bool IsFestivalWeek);

/// <summary>The extension point: one small rule per discount.</summary>
public interface IDiscountRule
{
    decimal Discount(Cart cart);
}

public sealed class LoyaltyDiscount : IDiscountRule
{
    public decimal Discount(Cart cart) => cart?.CustomerType switch
    {
        "gold" => cart.Total * 0.10m,
        "silver" => cart.Total * 0.05m,
        _ => 0m,
    };
}

public sealed class FestivalDiscount : IDiscountRule
{
    public decimal Discount(Cart cart) => cart?.IsFestivalWeek == true ? 100m : 0m;
}

/// <summary>Closed: this class never changes when a discount is added. Open: give it any rules you like.</summary>
public sealed class DiscountCalculator(IEnumerable<IDiscountRule> rules)
{
    public decimal Discount(Cart cart)
    {
        ArgumentNullException.ThrowIfNull(cart);
        return Math.Min(rules.Sum(rule => rule.Discount(cart)), cart.Total);
    }
}

/// <summary>Added in June: a new class. Nothing above was opened.</summary>
public sealed class FirstOrderDiscount(bool isFirstOrder) : IDiscountRule
{
    public decimal Discount(Cart cart) => isFirstOrder ? 50m : 0m;
}
