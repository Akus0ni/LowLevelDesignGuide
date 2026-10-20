namespace Lld.Samples.Patterns.Behavioral.ChainOfResponsibility.Before;

/// <summary>
/// Who handles an expense is one long <c>if</c> chain. A new rule (a CFO between director and board, a weekend check)
/// means editing this method, and the order of the checks is easy to break.
/// </summary>
public static class Approvals
{
    public static string Approve(Expense expense)
    {
        if (expense.Category == "gift card")
        {
            return "rejected by fraud check";
        }

        if (expense.Amount <= 1_000m)
        {
            return "approved by team lead";
        }

        if (expense.Amount <= 10_000m)
        {
            return "approved by manager";
        }

        return expense.Amount <= 100_000m ? "approved by director" : "nobody can approve this";
    }
}
