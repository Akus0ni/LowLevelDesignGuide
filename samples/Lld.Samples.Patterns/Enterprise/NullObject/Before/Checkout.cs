namespace Lld.Samples.Patterns.Enterprise.NullObject.Before;

public interface IDiscount
{
    decimal For(decimal total);
}

public interface IAuditLog
{
    void Write(string line);
}

/// <summary>
/// "No discount" and "no log" are written as <c>null</c>, so every use needs a check. Forget one and it's a
/// <see cref="NullReferenceException"/> in production, only for customers without a discount.
/// </summary>
public sealed class Checkout(IDiscount? discount, IAuditLog? log)
{
    public decimal Total(decimal subtotal)
    {
        var total = discount is null ? subtotal : subtotal - discount.For(subtotal);
        if (log is not null)
        {
            log.Write($"total {total}");
        }

        return total;
    }
}
