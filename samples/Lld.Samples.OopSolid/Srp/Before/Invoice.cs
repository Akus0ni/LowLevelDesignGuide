using System.Globalization;

namespace Lld.Samples.OopSolid.Srp.Before;

public sealed record InvoiceLine(string Item, int Quantity, decimal UnitPrice);

/// <summary>
/// Three bosses, one class: finance owns the maths, design owns the HTML, IT owns where files go. A change for any of
/// them reopens this class and risks the other two.
/// </summary>
public sealed class Invoice(string number, IReadOnlyList<InvoiceLine> lines)
{
    public string Number { get; } = number;

    public IReadOnlyList<InvoiceLine> Lines { get; } = lines;

    /// <summary>Finance: tax rules.</summary>
    public decimal Total() => Math.Round(Lines.Sum(l => l.Quantity * l.UnitPrice) * 1.18m, 2);

    /// <summary>Design: how it looks.</summary>
    public string ToHtml() =>
        $"<h1>Invoice {Number}</h1><p>Total: {Total().ToString(CultureInfo.InvariantCulture)}</p>";

    /// <summary>IT: where it is kept.</summary>
    public void SaveTo(string folder) => File.WriteAllText(Path.Combine(folder, $"{Number}.html"), ToHtml());
}
