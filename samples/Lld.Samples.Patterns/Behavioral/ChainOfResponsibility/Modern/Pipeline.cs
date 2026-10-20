namespace Lld.Samples.Patterns.Behavioral.ChainOfResponsibility.Modern;

/// <summary>A step gets the expense and the rest of the chain; it answers, or calls <c>next</c>.</summary>
public delegate string Step(Expense expense, Func<Expense, string> next);

/// <summary>
/// Modern .NET builds chains from delegates, exactly like ASP.NET Core middleware (<c>app.Use((context, next) =&gt; ...)</c>):
/// each step decides whether to call <c>next</c>.
/// </summary>
public static class Pipeline
{
    public static Func<Expense, string> Build(params Step[] steps)
    {
        Func<Expense, string> chain = _ => "nobody can approve this";
        for (var i = steps.Length - 1; i >= 0; i--)
        {
            var step = steps[i];
            var next = chain;
            chain = expense => step(expense, next);
        }

        return chain;
    }
}

public static class Steps
{
    public static readonly Step FraudCheck = (expense, next) =>
        expense.Category == "gift card" ? "rejected by fraud check" : next(expense);

    public static Step Approver(string role, decimal limit) => (expense, next) =>
        expense.Amount <= limit ? $"approved by {role}" : next(expense);
}
