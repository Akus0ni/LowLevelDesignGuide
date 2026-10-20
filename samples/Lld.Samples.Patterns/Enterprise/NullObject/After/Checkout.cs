namespace Lld.Samples.Patterns.Enterprise.NullObject.After;

public interface IDiscount
{
    decimal For(decimal total);
}

public interface IAuditLog
{
    void Write(string line);
}

/// <summary>A null object: a real implementation that does nothing, safely. One shared instance is enough.</summary>
public sealed class NoDiscount : IDiscount
{
    public static readonly NoDiscount Instance = new();

    private NoDiscount()
    {
    }

    public decimal For(decimal total) => 0m;
}

public sealed class SilentLog : IAuditLog
{
    public static readonly SilentLog Instance = new();

    private SilentLog()
    {
    }

    public void Write(string line)
    {
        // Deliberately nothing: this is the "no log" choice, not a missing one.
    }
}

/// <summary>Never null, so no checks: the code reads as the happy path.</summary>
public sealed class Checkout(IDiscount discount, IAuditLog log)
{
    public decimal Total(decimal subtotal)
    {
        var total = subtotal - discount.For(subtotal);
        log.Write($"total {total}");
        return total;
    }
}
