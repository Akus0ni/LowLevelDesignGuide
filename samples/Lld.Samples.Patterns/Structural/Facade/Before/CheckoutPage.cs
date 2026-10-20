namespace Lld.Samples.Patterns.Structural.Facade.Before;

/// <summary>
/// The page drives the whole subsystem itself: the right order, and putting the stock back when payment fails. The
/// mobile app and the admin tool copy these steps, and each copy can forget one.
/// </summary>
public sealed class CheckoutPage(Inventory inventory, Payments payments, Shipping shipping, Mailer mailer)
{
    public OrderResult Buy(string sku, string card, string address)
    {
        if (!inventory.Reserve(sku))
        {
            return new OrderResult(false, null);
        }

        if (payments.Charge(card, 10m) is null)
        {
            inventory.Release(sku);
            return new OrderResult(false, null);
        }

        var tracking = shipping.Book(address);
        mailer.Send($"Your {sku} is on its way: {tracking}");
        return new OrderResult(true, tracking);
    }
}
