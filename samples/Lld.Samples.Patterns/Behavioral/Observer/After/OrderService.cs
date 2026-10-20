namespace Lld.Samples.Patterns.Behavioral.Observer.After;

/// <summary>The observer: anything that wants to hear about placed orders.</summary>
public interface IOrderObserver
{
    void OrderPlaced(string orderId);
}

/// <summary>The subject: keeps a list of observers and tells each of them; it doesn't know who they are.</summary>
public sealed class OrderService
{
    private readonly List<IOrderObserver> _observers = [];

    public void Subscribe(IOrderObserver observer) => _observers.Add(observer);

    public void Unsubscribe(IOrderObserver observer) => _observers.Remove(observer);

    public void Place(string orderId)
    {
        // ... save the order ...
        foreach (var observer in _observers)
        {
            observer.OrderPlaced(orderId);
        }
    }
}

public sealed class EmailOnOrder(Mailer mailer) : IOrderObserver
{
    public void OrderPlaced(string orderId) => mailer.Send($"Order {orderId} confirmed");
}

public sealed class SmsOnOrder(Sms sms) : IOrderObserver
{
    public void OrderPlaced(string orderId) => sms.Send($"Order {orderId} confirmed");
}

public sealed class TrackOrder(Analytics analytics) : IOrderObserver
{
    public void OrderPlaced(string orderId) => analytics.Track("order_placed");
}
