using System.Text;

namespace Lld.Samples.Patterns.Behavioral.TemplateMethod.Modern;

/// <summary>The varying steps as data: a format is a record of small functions, no subclass needed.</summary>
public sealed record ReportFormat(string Header, Func<string, string> Row, string Footer = "");

/// <summary>
/// Composition instead of inheritance: the fixed steps live in one method that takes the format. Easier to test and
/// combine; the trade-off is that a format can't reach shared protected helpers the way a subclass can.
/// </summary>
public static class Reports
{
    public static readonly ReportFormat Csv = new("item\n", row => row);
    public static readonly ReportFormat Html = new("<ul>\n", row => $"<li>{row}</li>", "</ul>\n");

    public static string Render(IEnumerable<string> rows, ReportFormat format)
    {
        var text = new StringBuilder(format.Header);
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            text.Append(format.Row(row.Trim())).Append('\n');
        }

        return text.Append(format.Footer).ToString();
    }
}
