namespace Lld.Samples.OopSolid.DryKissYagni.Before;

/// <summary>
/// One business rule ("shipping is free from ₹500") written three times. When marketing changes it to ₹400, someone
/// has to find all three, and the e-mail was missed.
/// </summary>
public static class Cart
{
    public static decimal Shipping(decimal subtotal) => subtotal >= 500m ? 0m : 49m;
}

public static class Invoice
{
    public static decimal Shipping(decimal subtotal) => subtotal >= 500m ? 0m : 49m;
}

public static class OrderEmail
{
    public static string ShippingLine(decimal subtotal) => subtotal > 500m ? "Free shipping!" : "Shipping: ₹49";
}
