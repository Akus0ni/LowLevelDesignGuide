namespace Lld.Samples.Uml.Activity;

public enum WithdrawalOutcome
{
    WrongPin,
    NotEnoughMoney,
    Dispensed,
}

/// <summary>
/// The activity diagram's branches, in code: check the PIN, then the balance; if both pass, take the money from the
/// account, then dispense the cash and print the receipt (the two parallel steps after the fork).
/// </summary>
public sealed class CashMachine(string pin, decimal balance)
{
    public decimal Balance { get; private set; } = balance;

    public List<string> Done { get; } = [];

    public WithdrawalOutcome Withdraw(string enteredPin, decimal amount)
    {
        if (enteredPin != pin)
        {
            return WithdrawalOutcome.WrongPin;
        }

        if (amount > Balance)
        {
            return WithdrawalOutcome.NotEnoughMoney;
        }

        Balance -= amount;
        Done.Add("dispense cash");
        Done.Add("print receipt");
        return WithdrawalOutcome.Dispensed;
    }
}
