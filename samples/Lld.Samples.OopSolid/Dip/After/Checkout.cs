namespace Lld.Samples.OopSolid.Dip.After;

/// <summary>
/// The socket. Checkout owns it and says, in its own words, what it needs: "tell the customer". Mail, SMS and test
/// fakes all plug into it.
/// </summary>
public interface ICustomerNotifier
{
    void Notify(string customer, string message);
}

/// <summary>High-level policy: depends only on the socket it owns.</summary>
public sealed class CheckoutService(ICustomerNotifier notifier)
{
    public decimal Checkout(string customer, decimal total)
    {
        notifier.Notify(customer, $"Thanks! You paid {total}.");
        return total;
    }
}

/// <summary>A low-level detail that plugs in. It depends on checkout's interface, not the other way round.</summary>
public sealed class EmailNotifier(Action<string, string> sendMail) : ICustomerNotifier
{
    public void Notify(string customer, string message) => sendMail(customer, message);
}

/// <summary>Another plug: in a test, nothing leaves the process.</summary>
public sealed class RecordingNotifier : ICustomerNotifier
{
    public List<string> Sent { get; } = [];

    public void Notify(string customer, string message) => Sent.Add($"{customer}: {message}");
}
