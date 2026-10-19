using System.Globalization;
using System.Text;

namespace Lld.Samples.OopSolid.Abstraction.Before;

public sealed record Sale(string Region, decimal Amount);

/// <summary>
/// One method, four levels of detail: the business step ("publish the monthly report"), totals, CSV escaping and file
/// paths, all interleaved. The caller has to know where files go.
/// </summary>
public static class SalesReport
{
    public static void Publish(List<Sale> sales, DateTime month, string folder)
    {
        ArgumentNullException.ThrowIfNull(sales);
        var totals = new Dictionary<string, decimal>();
        foreach (var sale in sales)
        {
            if (!totals.ContainsKey(sale.Region))
            {
                totals[sale.Region] = 0;
            }

            totals[sale.Region] += sale.Amount;
        }

        var csv = new StringBuilder("Region,Total\n");
        foreach (var (region, total) in totals.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            csv.Append(region.Replace(",", " ", StringComparison.Ordinal)).Append(',')
                .Append(total.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        var name = "sales-" + month.ToString("yyyy-MM", CultureInfo.InvariantCulture) + ".csv";
        File.WriteAllText(Path.Combine(folder, name), csv.ToString());
    }
}

/// <summary>A storage "abstraction" that is really the file system with an interface in front of it.</summary>
public interface IFileStorage
{
    string RootFolder { get; }

    FileStream OpenWrite(string fullPath);

    bool Exists(string fullPath);
}
