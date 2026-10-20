namespace Lld.Samples.Patterns.Enterprise.UnitOfWork.After;

/// <summary>
/// The unit of work: repositories only record what should change; <see cref="Commit"/> writes it all in one
/// transaction, so a failure leaves nothing half-saved.
/// </summary>
public sealed class UnitOfWork(Store store)
{
    private readonly List<Action<Store>> _changes = [];

    public void Register(Action<Store> change) => _changes.Add(change);

    public void Commit()
    {
        store.Apply(_changes);
        _changes.Clear();
    }
}

public sealed class OrderRepository(UnitOfWork work)
{
    public void Add(string sku) => work.Register(store => store.WriteOrder(sku));
}

public sealed class StockRepository(UnitOfWork work)
{
    public void Take(string sku) => work.Register(store => store.WriteStock(sku, -1));
}

/// <summary>The use case makes its changes, then commits once.</summary>
public sealed class PlaceOrder(OrderRepository orders, StockRepository stock, UnitOfWork work)
{
    public void Execute(string sku)
    {
        orders.Add(sku);
        stock.Take(sku);
        work.Commit();
    }
}
