namespace Lld.Samples.Patterns.Behavioral.Observer.Before;

/// <summary>
/// Placing an order calls everyone who cares, by name. A new listener (loyalty points) means editing the order code,
/// and the order can't be tested without a mailer, an SMS gateway and analytics.
/// </summary>
public sealed class OrderService(Mailer mailer, Sms sms, Analytics analytics)
{
    public void Place(string orderId)
    {
        // ... save the order ...
        mailer.Send($"Order {orderId} confirmed");
        sms.Send($"Order {orderId} confirmed");
        analytics.Track("order_placed");
    }
}
