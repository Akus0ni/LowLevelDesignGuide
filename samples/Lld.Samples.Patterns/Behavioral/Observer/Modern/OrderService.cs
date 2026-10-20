namespace Lld.Samples.Patterns.Behavioral.Observer.Modern;

public sealed class OrderPlacedEventArgs(string orderId) : EventArgs
{
    public string OrderId { get; } = orderId;
}

/// <summary>
/// C# has Observer built in: an <c>event</c> is the observer list, <c>+=</c> subscribes and <c>-=</c> unsubscribes.
/// For streams of events over time use <see cref="IObservable{T}"/> (Rx) or <c>System.Threading.Channels</c>.
/// </summary>
public sealed class OrderService
{
    public event EventHandler<OrderPlacedEventArgs>? Placed;

    public void Place(string orderId)
    {
        // ... save the order ...
        Placed?.Invoke(this, new OrderPlacedEventArgs(orderId));
    }
}
