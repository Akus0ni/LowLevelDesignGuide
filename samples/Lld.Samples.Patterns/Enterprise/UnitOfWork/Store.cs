namespace Lld.Samples.Patterns.Enterprise.UnitOfWork;

/// <summary>
/// Stands in for the database. A write can fail (a lost connection, a constraint); a batch is applied all or nothing,
/// like a transaction. Shared by every version.
/// </summary>
public sealed class Store
{
    public List<string> Orders { get; } = [];

    public Dictionary<string, int> Stock { get; } = new(StringComparer.Ordinal) { ["pen"] = 3 };

    /// <summary>Makes the next stock write throw, to show what a failure in the middle does.</summary>
    public bool FailNextStockWrite { get; set; }

    public void WriteOrder(string sku) => Orders.Add(sku);

    public void WriteStock(string sku, int change)
    {
        if (FailNextStockWrite)
        {
            FailNextStockWrite = false;
            throw new IOException("The connection dropped.");
        }

        Stock[sku] += change;
    }

    /// <summary>Applies every change or none of them (a transaction).</summary>
    public void Apply(IReadOnlyList<Action<Store>> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        string[] ordersBefore = [.. Orders];
        var stockBefore = new Dictionary<string, int>(Stock, StringComparer.Ordinal);
        try
        {
            foreach (var change in changes)
            {
                change(this);
            }
        }
        catch (IOException)
        {
            Orders.Clear();
            Orders.AddRange(ordersBefore);
            Stock.Clear();
            foreach (var (sku, count) in stockBefore)
            {
                Stock[sku] = count;
            }

            throw;
        }
    }
}
