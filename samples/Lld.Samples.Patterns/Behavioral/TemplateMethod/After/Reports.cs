using System.Text;

namespace Lld.Samples.Patterns.Behavioral.TemplateMethod.After;

/// <summary>
/// The template method <see cref="Render"/> fixes the steps once and can't be overridden. Subclasses fill in the
/// steps that differ: <see cref="Row"/> must be written, <see cref="Header"/> and <see cref="Footer"/> are optional hooks.
/// </summary>
public abstract class Report
{
    public string Render(IEnumerable<string> rows)
    {
        var text = new StringBuilder(Header());
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            text.Append(Row(row.Trim())).Append('\n');
        }

        return text.Append(Footer()).ToString();
    }

    protected abstract string Row(string row);

    protected virtual string Header() => string.Empty;

    protected virtual string Footer() => string.Empty;
}

public sealed class CsvReport : Report
{
    protected override string Header() => "item\n";

    protected override string Row(string row) => row;
}

public sealed class HtmlReport : Report
{
    protected override string Header() => "<ul>\n";

    protected override string Row(string row) => $"<li>{row}</li>";

    protected override string Footer() => "</ul>\n";
}
