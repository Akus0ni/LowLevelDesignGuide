namespace Lld.Samples.Patterns.Enterprise.Repository;

/// <summary>The domain object. Shared by every version.</summary>
public sealed class Order(Guid id, string customer)
{
    public Guid Id { get; } = id;

    public string Customer { get; } = customer;

    public bool Shipped { get; private set; }

    public void Ship() => Shipped = true;
}

/// <summary>Stands in for the database: a table of orders. Shared by every version.</summary>
public sealed class Database
{
    public List<Order> Orders { get; } = [];
}
