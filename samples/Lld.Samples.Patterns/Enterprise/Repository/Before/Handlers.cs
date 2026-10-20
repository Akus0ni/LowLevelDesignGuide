namespace Lld.Samples.Patterns.Enterprise.Repository.Before;

/// <summary>
/// Every handler queries the database itself. "Pending orders" is written in several places (already slightly
/// different), the handlers can't be tested without a database, and a schema change touches all of them.
/// </summary>
public sealed class ShipOrder(Database db)
{
    public bool Execute(Guid orderId)
    {
        var order = db.Orders.SingleOrDefault(o => o.Id == orderId && !o.Shipped);
        order?.Ship();
        return order is not null;
    }
}

public sealed class PendingOrdersReport(Database db)
{
    public IReadOnlyList<string> Customers() => [.. db.Orders.Where(o => o.Shipped == false).Select(o => o.Customer)];
}
