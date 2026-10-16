namespace Lld.Samples.OopSolid.Encapsulation.Before;

public sealed record Transaction(decimal Amount, DateTimeOffset At);

/// <summary>A bag of data: every property can be set to anything, by anyone.</summary>
public sealed class BankAccount
{
    public string Owner { get; set; } = "";

    public decimal Balance { get; set; }

    public List<Transaction> Transactions { get; set; } = [];
}

/// <summary>The rules live outside the data, so every caller has to know and repeat them.</summary>
public static class AccountService
{
    public static void Withdraw(BankAccount account, decimal amount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Withdraw a positive amount.");
        }

        if (account.Balance < amount)
        {
            throw new InvalidOperationException($"Balance {account.Balance} is less than {amount}.");
        }

        account.Balance -= amount;
        account.Transactions.Add(new Transaction(-amount, now));
    }
}

/// <summary>Written months later by someone else: it changes the balance and forgets the history.</summary>
public static class RefundHandler
{
    public static void Refund(BankAccount account, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(account);
        account.Balance += amount;
    }
}
