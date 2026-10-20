namespace Lld.Samples.Patterns.Enterprise.Repository.After;

/// <summary>
/// The repository: a collection-like interface for one aggregate, in the domain's words. Callers ask for "pending
/// orders"; how that is stored and queried stays behind it.
/// </summary>
public interface IOrderRepository
{
    Order? FindPending(Guid id);

    IReadOnlyList<Order> Pending();

    void Add(Order order);
}

/// <summary>One implementation per storage: this one over the in-memory table; another over EF Core.</summary>
public sealed class OrderRepository(Database db) : IOrderRepository
{
    public Order? FindPending(Guid id) => db.Orders.SingleOrDefault(o => o.Id == id && !o.Shipped);

    public IReadOnlyList<Order> Pending() => [.. db.Orders.Where(o => !o.Shipped)];

    public void Add(Order order) => db.Orders.Add(order);
}

public sealed class ShipOrder(IOrderRepository orders)
{
    public bool Execute(Guid orderId)
    {
        var order = orders.FindPending(orderId);
        order?.Ship();
        return order is not null;
    }
}

public sealed class PendingOrdersReport(IOrderRepository orders)
{
    public IReadOnlyList<string> Customers() => [.. orders.Pending().Select(o => o.Customer)];
}
