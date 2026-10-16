namespace Lld.Samples.OopSolid.DryKissYagni.Before;

/// <summary>
/// KISS, broken: a rule engine with a registry and named factories, for the one rule the business has (10% off for
/// members).
/// </summary>
public sealed class DiscountEngine
{
    private readonly Dictionary<string, Func<decimal, decimal>> _rules = new(StringComparer.Ordinal);

    public DiscountEngine Register(string name, Func<decimal, decimal> rule)
    {
        _rules[name] = rule;
        return this;
    }

    public decimal Apply(string ruleName, decimal total) =>
        _rules.TryGetValue(ruleName, out var rule) ? rule(total) : total;
}

/// <summary>YAGNI, broken: three exporters "for later". Only CSV was ever asked for; the others just throw.</summary>
public interface IReportExporter
{
    string Export(IReadOnlyList<string> rows);
}

public sealed class CsvExporter : IReportExporter
{
    public string Export(IReadOnlyList<string> rows) => string.Join('\n', rows ?? []);
}

public sealed class ExcelExporter : IReportExporter
{
    public string Export(IReadOnlyList<string> rows) => throw new NotImplementedException("Maybe next year.");
}

public sealed class PdfExporter : IReportExporter
{
    public string Export(IReadOnlyList<string> rows) => throw new NotImplementedException("Maybe next year.");
}
