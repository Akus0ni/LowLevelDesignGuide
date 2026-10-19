namespace Lld.Samples.OopSolid.Abstraction.Language;

public sealed record Customer(string Name, string City);

/// <summary>
/// <c>IEnumerable&lt;T&gt;</c> promises "a sequence" but hides <i>when</i> the work happens: this one is evaluated again
/// every time someone enumerates it, so a caller that counts it and then loops over it runs the filter twice.
/// </summary>
public sealed class CustomerDirectory(IReadOnlyList<Customer> customers)
{
    public int Evaluations { get; private set; }

    public IEnumerable<Customer> InCity(string city) =>
        customers.Where(customer =>
        {
            Evaluations++;
            return customer.City == city;
        });

    /// <summary>Says what it is: a finished list, worked out once.</summary>
    public IReadOnlyList<Customer> ListInCity(string city) => [.. InCity(city)];
}
