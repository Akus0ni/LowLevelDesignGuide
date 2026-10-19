namespace Lld.Samples.OopSolid.InterfacesVsAbstractClasses.After;

/// <summary>The contract: anything that can pay. It says nothing about cards, numbers or how.</summary>
public interface IPaymentMethod
{
    string Label { get; }

    bool Pay(decimal amount);
}

/// <summary>Shared code for the card family only: masking and the limit check. A convenience, not the contract.</summary>
public abstract class CardPayment(string cardNumber, decimal limit) : IPaymentMethod
{
    public string Label => $"**** {cardNumber[^4..]}";

    public bool Pay(decimal amount) => amount <= limit && Authorise(amount);

    /// <summary>The one step that differs between cards.</summary>
    protected abstract bool Authorise(decimal amount);
}

public sealed class CreditCard(string cardNumber) : CardPayment(cardNumber, limit: 5000)
{
    protected override bool Authorise(decimal amount) => true;
}

public sealed class DebitCard(string cardNumber, decimal balance) : CardPayment(cardNumber, limit: 2000)
{
    protected override bool Authorise(decimal amount) => amount <= balance;
}

/// <summary>Not a card at all, and it doesn't have to pretend to be one.</summary>
public sealed class UpiWallet(string upiId) : IPaymentMethod
{
    public string Label => upiId;

    public bool Pay(decimal amount) => amount <= 1000;
}
