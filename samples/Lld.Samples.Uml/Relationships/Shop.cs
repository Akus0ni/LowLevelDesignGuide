namespace Lld.Samples.Uml.Relationships;

/// <summary>Realization: <see cref="Product"/> promises what this interface says.</summary>
public interface IPriced
{
    decimal Price { get; }
}

public class Product(string name, decimal price) : IPriced
{
    public string Name { get; } = name;

    public decimal Price { get; } = price;
}

/// <summary>Inheritance: a digital product is a product.</summary>
public sealed class DigitalProduct(string name, decimal price, Uri download) : Product(name, price)
{
    public Uri Download { get; } = download;
}

/// <summary>Composition: the order makes its own lines and nobody else holds them; they go when the order goes.</summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public IReadOnlyList<OrderLine> Lines => _lines;

    public void Add(Product product, int quantity) => _lines.Add(new OrderLine(product, quantity));

    /// <summary>Dependency: the calculator is used for a moment and never kept.</summary>
    public decimal Total(TaxCalculator tax) => tax.WithTax(_lines.Sum(line => line.Subtotal));
}

/// <summary>Association: a line knows its product. Only an order can make one (internal constructor).</summary>
public sealed class OrderLine
{
    internal OrderLine(Product product, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Product = product;
        Quantity = quantity;
    }

    public Product Product { get; }

    public int Quantity { get; }

    public decimal Subtotal => Product.Price * Quantity;
}

/// <summary>Aggregation: orders are handed in; they exist before the customer keeps them and after it forgets them.</summary>
public sealed class Customer(string name)
{
    private readonly List<Order> _orders = [];

    public string Name { get; } = name;

    public IReadOnlyList<Order> Orders => _orders;

    public void Keep(Order order) => _orders.Add(order);

    public void Forget(Order order) => _orders.Remove(order);
}

public sealed class TaxCalculator(decimal rate)
{
    public decimal WithTax(decimal amount) => amount * (1 + rate);
}
