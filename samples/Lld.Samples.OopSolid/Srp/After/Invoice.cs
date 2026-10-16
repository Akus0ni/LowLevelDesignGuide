using System.Globalization;

namespace Lld.Samples.OopSolid.Srp.After;

public sealed record InvoiceLine(string Item, int Quantity, decimal UnitPrice);

/// <summary>Finance's class: the numbers and the tax rule, nothing else.</summary>
public sealed class Invoice(string number, IReadOnlyList<InvoiceLine> lines)
{
    private const decimal TaxRate = 0.18m;

    public string Number { get; } = number;

    public IReadOnlyList<InvoiceLine> Lines { get; } = lines;

    public decimal Total => Math.Round(Lines.Sum(l => l.Quantity * l.UnitPrice) * (1 + TaxRate), 2);
}

/// <summary>Design's class: how an invoice looks.</summary>
public static class InvoiceHtml
{
    public static string Render(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        return $"<h1>Invoice {invoice.Number}</h1><p>Total: {invoice.Total.ToString(CultureInfo.InvariantCulture)}</p>";
    }
}

/// <summary>IT's class: where documents are kept.</summary>
public sealed class InvoiceArchive(string folder)
{
    public void Save(string number, string html) => File.WriteAllText(Path.Combine(folder, $"{number}.html"), html);
}
