namespace Lld.Samples.OopSolid.DryKissYagni.After;

/// <summary>The rule, written once. Cart, invoice and e-mail all ask it.</summary>
public static class ShippingPolicy
{
    public const decimal FreeFrom = 500m;
    public const decimal Fee = 49m;

    public static bool IsFree(decimal subtotal) => subtotal >= FreeFrom;

    public static decimal Cost(decimal subtotal) => IsFree(subtotal) ? 0m : Fee;
}

public static class Cart
{
    public static decimal Shipping(decimal subtotal) => ShippingPolicy.Cost(subtotal);
}

public static class Invoice
{
    public static decimal Shipping(decimal subtotal) => ShippingPolicy.Cost(subtotal);
}

public static class OrderEmail
{
    public static string ShippingLine(decimal subtotal) =>
        ShippingPolicy.IsFree(subtotal) ? "Free shipping!" : $"Shipping: ₹{ShippingPolicy.Fee}";
}
