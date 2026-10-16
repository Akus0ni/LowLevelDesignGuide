namespace Lld.Samples.OopSolid.Encapsulation.After;

public sealed record Transaction(decimal Amount, DateTimeOffset At);

/// <summary>
/// The account guards its own rules: the balance never goes below zero and always equals the sum of its transactions.
/// Callers say what they want (deposit, withdraw); they can't reach in and change the numbers.
/// </summary>
public sealed class BankAccount
{
    private readonly List<Transaction> _transactions = [];

    public BankAccount(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        Owner = owner;
    }

    public string Owner { get; }

    public decimal Balance { get; private set; }

    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    public void Deposit(decimal amount, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Record(amount, now);
    }

    public void Withdraw(decimal amount, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (amount > Balance)
        {
            throw new InvalidOperationException($"Balance {Balance} is less than {amount}.");
        }

        Record(-amount, now);
    }

    /// <summary>The only place the balance changes, so the history can't drift from it.</summary>
    private void Record(decimal amount, DateTimeOffset now)
    {
        Balance += amount;
        _transactions.Add(new Transaction(amount, now));
    }
}
