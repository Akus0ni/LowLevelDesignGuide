namespace Lld.Samples.OopSolid.LawOfDemeter.Before;

public sealed class Wallet
{
    public decimal Cash { get; set; }
}

public sealed class Customer(string name, Wallet wallet)
{
    public string Name { get; } = name;

    public Wallet Wallet { get; } = wallet;
}

/// <summary>The paperboy reaches into the customer's pocket, takes the wallet and helps himself.</summary>
public sealed class Paperboy
{
    public decimal Collected { get; private set; }

    public void Collect(Customer customer, decimal price)
    {
        ArgumentNullException.ThrowIfNull(customer);
        customer.Wallet.Cash -= price;
        Collected += price;
    }
}
