namespace Lld.Samples.OopSolid.InterfacesVsAbstractClasses.Before;

/// <summary>An abstract class used as a contract: every payment method must now be a kind of card.</summary>
public abstract class PaymentMethod
{
    protected PaymentMethod(string cardNumber) => CardNumber = cardNumber;

    public string CardNumber { get; }

    public string Masked => $"**** {CardNumber[^4..]}";

    public abstract bool Pay(decimal amount);
}

public sealed class CreditCard(string cardNumber) : PaymentMethod(cardNumber)
{
    public override bool Pay(decimal amount) => amount <= 5000;
}

/// <summary>A wallet has no card number, but the base class demands one, so it invents a fake.</summary>
public sealed class UpiWallet(string upiId) : PaymentMethod("0000000000000000")
{
    public string UpiId { get; } = upiId;

    public override bool Pay(decimal amount) => amount <= 1000;
}
