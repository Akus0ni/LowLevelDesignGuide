namespace Lld.Samples.OopSolid.LawOfDemeter.After;

/// <summary>The customer decides how to pay; the wallet is their business.</summary>
public sealed class Customer(string name, decimal cash)
{
    private decimal _cash = cash;

    public string Name { get; } = name;

    /// <summary>Pays if they can. Cash, card or IOU later: the paperboy never needs to know.</summary>
    public bool TryPay(decimal amount)
    {
        if (amount > _cash)
        {
            return false;
        }

        _cash -= amount;
        return true;
    }
}

/// <summary>The paperboy talks only to the customer in front of him.</summary>
public sealed class Paperboy
{
    public decimal Collected { get; private set; }

    public bool Collect(Customer customer, decimal price)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (!customer.TryPay(price))
        {
            return false;
        }

        Collected += price;
        return true;
    }
}
