namespace Lld.Samples.Patterns.Enterprise.Specification.After;

/// <summary>A specification: one business rule, as an object that answers "does this item qualify?".</summary>
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T item);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);
}

internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T item) => left.IsSatisfiedBy(item) && right.IsSatisfiedBy(item);
}

internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T item) => left.IsSatisfiedBy(item) || right.IsSatisfiedBy(item);
}

/// <summary>Each rule is written once and named in the business's words.</summary>
public sealed class ActiveCustomer : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer item) => item.IsActive;
}

public sealed class InCountry(string country) : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer item) => item.Country == country;
}

public sealed class SpentOver(decimal amount) : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer item) => item.Spent > amount;
}

/// <summary>The repository has one query method; the specification says what to find.</summary>
public sealed class CustomerRepository(IReadOnlyList<Customer> customers)
{
    public IReadOnlyList<Customer> Find(Specification<Customer> specification) =>
        [.. customers.Where(specification.IsSatisfiedBy)];
}
