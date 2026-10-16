namespace Lld.Samples.OopSolid.Isp.Before;

/// <summary>One fat interface for every office machine.</summary>
public interface IOfficeMachine
{
    string Print(string document);

    string Scan(string page);

    string Fax(string document, string number);
}

/// <summary>A simple printer has to sign for scanning and faxing, and can only refuse at runtime.</summary>
public sealed class BasicPrinter : IOfficeMachine
{
    public string Print(string document) => $"printed {document}";

    public string Scan(string page) => throw new NotSupportedException("This printer can't scan.");

    public string Fax(string document, string number) => throw new NotSupportedException("This printer can't fax.");
}
