namespace Lld.Samples.Patterns.Structural.Facade.After;

/// <summary>
/// The facade: one simple method in front of the subsystem. It knows the order of the steps and how to undo them;
/// the subsystem classes stay usable on their own when someone needs more control.
/// </summary>
public sealed class OrderFacade(Inventory inventory, Payments payments, Shipping shipping, Mailer mailer)
{
    public OrderResult PlaceOrder(string sku, string card, string address)
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

/// <summary>Every client now asks the facade; none of them knows the steps.</summary>
public sealed class CheckoutPage(OrderFacade orders)
{
    public OrderResult Buy(string sku, string card, string address) => orders.PlaceOrder(sku, card, address);
}
