namespace Lld.Samples.Patterns.Creational.FactoryMethod.Modern;

/// <summary>
/// Modern C#: the factory method is a delegate the caller passes in, so no subclass is needed:
/// <c>new ReportExporter(() => new CsvWriter())</c>.
/// </summary>
public sealed class ReportExporter(Func<IReportWriter> createWriter)
{
    public string Export(IReadOnlyList<string> rows) => createWriter().Write(rows);
}
