namespace Lld.Samples.Patterns.Creational.FactoryMethod.Before;

/// <summary>The exporter picks the writer itself. Every new format means opening this method again.</summary>
public static class ReportExporter
{
    public static string Export(string format, IReadOnlyList<string> rows)
    {
        IReportWriter writer = format switch
        {
            "csv" => new CsvWriter(),
            "markdown" => new MarkdownWriter(),
            _ => throw new ArgumentException($"Unknown format '{format}'.", nameof(format)),
        };
        return writer.Write(rows);
    }
}
