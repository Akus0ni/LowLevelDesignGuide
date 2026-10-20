using System.Linq.Expressions;

namespace Lld.Samples.Patterns.Enterprise.Specification.Modern;

/// <summary>
/// A specification built on an expression tree: the same rule filters a list in memory (<see cref="IsSatisfiedBy"/>)
/// and, passed to EF Core's <c>Where(spec.Criteria)</c>, becomes SQL. Libraries like Ardalis.Specification grow this idea.
/// </summary>
public sealed class Spec<T>(Expression<Func<T, bool>> criteria)
{
    private readonly Func<T, bool> _compiled = criteria.Compile();

    public Expression<Func<T, bool>> Criteria => criteria;

    public bool IsSatisfiedBy(T item) => _compiled(item);

    /// <summary>Joins two rules into one expression, so a database can still run it.</summary>
    public Spec<T> And(Spec<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var item = criteria.Parameters[0];
        var otherBody = new SwapParameter(other.Criteria.Parameters[0], item).Visit(other.Criteria.Body);
        return new Spec<T>(Expression.Lambda<Func<T, bool>>(Expression.AndAlso(criteria.Body, otherBody), item));
    }

    private sealed class SwapParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}

public static class CustomerSpecs
{
    public static readonly Spec<Customer> Active = new(c => c.IsActive);

    public static Spec<Customer> InCountry(string country) => new(c => c.Country == country);
}
