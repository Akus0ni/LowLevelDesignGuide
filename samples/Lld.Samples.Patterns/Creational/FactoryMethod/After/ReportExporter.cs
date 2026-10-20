namespace Lld.Samples.Patterns.Creational.FactoryMethod.After;

/// <summary>
/// The creator: exporting is written once; <em>which</em> writer to make is left to a subclass, through the factory
/// method <see cref="CreateWriter"/>.
/// </summary>
public abstract class ReportExporter
{
    public string Export(IReadOnlyList<string> rows) => CreateWriter().Write(rows);

    protected abstract IReportWriter CreateWriter();
}

public sealed class CsvExporter : ReportExporter
{
    protected override IReportWriter CreateWriter() => new CsvWriter();
}

public sealed class MarkdownExporter : ReportExporter
{
    protected override IReportWriter CreateWriter() => new MarkdownWriter();
}
