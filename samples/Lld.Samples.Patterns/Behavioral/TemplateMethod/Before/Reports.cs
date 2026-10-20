using System.Text;

namespace Lld.Samples.Patterns.Behavioral.TemplateMethod.Before;

/// <summary>
/// Two reports repeat the same steps (skip blank rows, header, each row, footer) with small differences. A fix to the
/// steps, like trimming rows, must be made twice, and the HTML one already forgot.
/// </summary>
public static class CsvReport
{
    public static string Render(IEnumerable<string> rows)
    {
        var text = new StringBuilder("item\n");
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            text.Append(row.Trim()).Append('\n');
        }

        return text.ToString();
    }
}

public static class HtmlReport
{
    public static string Render(IEnumerable<string> rows)
    {
        var text = new StringBuilder("<ul>\n");
        foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            text.Append("<li>").Append(row).Append("</li>\n");
        }

        return text.Append("</ul>\n").ToString();
    }
}
