namespace Lld.Samples.Patterns.Enterprise.Repository.Modern;

/// <summary>
/// With EF Core, many teams skip their own repository classes: the <c>DbContext</c> is already the unit of work and
/// each <c>DbSet&lt;T&gt;</c> a repository. Named queries as extension methods on <see cref="IQueryable{T}"/> keep the
/// queries in one place, and EF Core still turns them into SQL.
/// </summary>
public static class OrderQueries
{
    public static IQueryable<Order> Pending(this IQueryable<Order> orders) => orders.Where(o => !o.Shipped);

    public static IQueryable<Order> ForCustomer(this IQueryable<Order> orders, string customer) =>
        orders.Where(o => o.Customer == customer);
}
