namespace Lld.Samples.Patterns.Behavioral.ChainOfResponsibility.After;

/// <summary>
/// A handler: tries to deal with the expense; if it can't, passes it to the next handler. The sender only knows the
/// first link.
/// </summary>
public abstract class ExpenseHandler(ExpenseHandler? next)
{
    public string Handle(Expense expense) => TryHandle(expense) ?? next?.Handle(expense) ?? "nobody can approve this";

    /// <summary>The answer, or null to pass the expense on.</summary>
    protected abstract string? TryHandle(Expense expense);
}

/// <summary>Stops the chain early: suspicious expenses never reach an approver.</summary>
public sealed class FraudCheck(ExpenseHandler? next) : ExpenseHandler(next)
{
    protected override string? TryHandle(Expense expense) => expense.Category == "gift card" ? "rejected by fraud check" : null;
}

public sealed class Approver(string role, decimal limit, ExpenseHandler? next) : ExpenseHandler(next)
{
    protected override string? TryHandle(Expense expense) => expense.Amount <= limit ? $"approved by {role}" : null;
}

public static class Approvals
{
    /// <summary>The chain is built in one place; adding a CFO is one more link.</summary>
    public static ExpenseHandler Chain() =>
        new FraudCheck(
            new Approver("team lead", 1_000m,
                new Approver("manager", 10_000m,
                    new Approver("director", 100_000m, next: null))));
}
