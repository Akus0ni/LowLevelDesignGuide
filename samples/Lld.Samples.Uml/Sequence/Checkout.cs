namespace Lld.Samples.Uml.Sequence;

/// <summary>
/// One call, drawn as a sequence diagram: ask the cart for its total, charge the card, and only if the payment is
/// approved, send a receipt.
/// </summary>
public sealed class CheckoutService(IPaymentGateway payments, IReceiptSender receipts)
{
    /// <returns>The receipt, or <c>null</c> when the card was declined (nothing is sent).</returns>
    public Receipt? Checkout(Cart cart, string card)
    {
        var total = cart.Total();
        var payment = payments.Charge(card, total);
        if (!payment.Approved)
        {
            return null;
        }

        var receipt = new Receipt(payment.Id, total);
        receipts.Send(receipt);
        return receipt;
    }
}

public sealed class Cart
{
    private readonly List<decimal> _prices = [];

    public void Add(decimal price) => _prices.Add(price);

    public decimal Total() => _prices.Sum();
}

public interface IPaymentGateway
{
    Payment Charge(string card, decimal amount);
}

public interface IReceiptSender
{
    void Send(Receipt receipt);
}

public sealed record Payment(string Id, bool Approved);

public sealed record Receipt(string PaymentId, decimal Total);
