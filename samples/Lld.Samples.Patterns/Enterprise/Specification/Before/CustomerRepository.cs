namespace Lld.Samples.Patterns.Enterprise.Specification.Before;

/// <summary>
/// One method per combination of filters. Marketing asks for "active Indian customers who spent over ₹10,000" and
/// a fourth method appears; the "active" rule is now written in three places.
/// </summary>
public sealed class CustomerRepository(IReadOnlyList<Customer> customers)
{
    public IReadOnlyList<Customer> ActiveInCountry(string country) =>
        [.. customers.Where(c => c.IsActive && c.Country == country)];

    public IReadOnlyList<Customer> ActiveWhoSpentOver(decimal amount) =>
        [.. customers.Where(c => c.IsActive && c.Spent > amount)];

    public IReadOnlyList<Customer> ActiveInCountryWhoSpentOver(string country, decimal amount) =>
        [.. customers.Where(c => c.IsActive && c.Country == country && c.Spent > amount)];
}
