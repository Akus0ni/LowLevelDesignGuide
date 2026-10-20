namespace Lld.Samples.Patterns.Enterprise.UnitOfWork.Before;

/// <summary>Each repository saves the moment it is called.</summary>
public sealed class OrderRepository(Store store)
{
    public void Add(string sku) => store.WriteOrder(sku);
}

public sealed class StockRepository(Store store)
{
    public void Take(string sku) => store.WriteStock(sku, -1);
}

/// <summary>
/// Two saves, one after the other. If the second one fails, the order is already saved but the stock never went
/// down: the data no longer agrees with itself.
/// </summary>
public sealed class PlaceOrder(OrderRepository orders, StockRepository stock)
{
    public void Execute(string sku)
    {
        orders.Add(sku);
        stock.Take(sku);
    }
}
