using System.Linq.Expressions;

namespace Lld.Samples.Patterns.Behavioral.Interpreter.Modern;

/// <summary>
/// .NET ships an interpreter: <c>System.Linq.Expressions</c>. Build the rule as an expression tree, then compile it to
/// a fast delegate, or hand the same tree to EF Core, which translates it to SQL.
/// </summary>
public static class RuleExpressions
{
    private static readonly ParameterExpression CartParameter = Expression.Parameter(typeof(Cart), "cart");

    public static Expression Member => Expression.Property(CartParameter, nameof(Cart.IsMember));

    public static Expression TotalOver(decimal amount) =>
        Expression.GreaterThan(Expression.Property(CartParameter, nameof(Cart.Total)), Expression.Constant(amount));

    public static Expression<Func<Cart, bool>> Lambda(Expression body) =>
        Expression.Lambda<Func<Cart, bool>>(body, CartParameter);
}
