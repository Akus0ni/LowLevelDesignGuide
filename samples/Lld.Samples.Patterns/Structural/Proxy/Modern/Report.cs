namespace Lld.Samples.Patterns.Structural.Proxy.Modern;

/// <summary>
/// A virtual proxy is built in: <see cref="Lazy{T}"/> stands in for a costly object and creates it on first use,
/// once, thread-safely. EF Core's lazy-loading proxies do the same for navigation properties.
/// </summary>
public sealed class Report(Func<IReadOnlyList<string>> loadRows)
{
    private readonly Lazy<IReadOnlyList<string>> _rows = new(loadRows);

    public bool IsLoaded => _rows.IsValueCreated;

    public int RowCount => _rows.Value.Count;
}
