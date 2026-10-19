using System.Globalization;
using System.Text;

namespace Lld.Samples.OopSolid.Abstraction.After;

public sealed record Sale(string Region, decimal Amount);

public sealed record RegionTotal(string Region, decimal Total);

/// <summary>
/// The public method reads as the business step; each private method handles one level of detail below it. Where the
/// report is kept is the store's business, not the report's.
/// </summary>
public sealed class SalesReport(IDocumentStore store)
{
    public Task PublishAsync(IEnumerable<Sale> sales, DateOnly month, CancellationToken cancellationToken)
    {
        var totals = TotalsByRegion(sales);
        return store.SaveAsync(DocumentName(month), ToCsv(totals), cancellationToken);
    }

    private static List<RegionTotal> TotalsByRegion(IEnumerable<Sale> sales) =>
        [.. sales
            .GroupBy(sale => sale.Region, StringComparer.Ordinal)
            .Select(group => new RegionTotal(group.Key, group.Sum(sale => sale.Amount)))
            .OrderBy(total => total.Region, StringComparer.Ordinal)];

    private static string ToCsv(IEnumerable<RegionTotal> totals)
    {
        var csv = new StringBuilder("Region,Total\n");
        foreach (var total in totals)
        {
            csv.Append(CultureInfo.InvariantCulture, $"{Escape(total.Region)},{total.Total}\n");
        }

        return csv.ToString();
    }

    private static string Escape(string value) => value.Replace(",", " ", StringComparison.Ordinal);

    private static string DocumentName(DateOnly month) => $"sales-{month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}.csv";
}
