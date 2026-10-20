namespace Lld.Samples.Patterns.Creational.FactoryMethod;

/// <summary>The product: turns report rows into text. Shared by the Before, After and Modern versions.</summary>
public interface IReportWriter
{
    string Write(IReadOnlyList<string> rows);
}

public sealed class CsvWriter : IReportWriter
{
    public string Write(IReadOnlyList<string> rows) => string.Join('\n', ["item", .. rows]);
}

public sealed class MarkdownWriter : IReportWriter
{
    public string Write(IReadOnlyList<string> rows) => string.Join('\n', ["| item |", "|---|", .. rows.Select(r => $"| {r} |")]);
}
