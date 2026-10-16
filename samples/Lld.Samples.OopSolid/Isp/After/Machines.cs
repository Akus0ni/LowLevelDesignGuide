namespace Lld.Samples.OopSolid.Isp.After;

/// <summary>Small role interfaces: each says one thing a machine can do.</summary>
public interface IPrinter
{
    string Print(string document);
}

public interface IScanner
{
    string Scan(string page);
}

public interface IFax
{
    string Fax(string document, string number);
}

/// <summary>Signs only for what it does. Asking it to scan doesn't compile.</summary>
public sealed class BasicPrinter : IPrinter
{
    public string Print(string document) => $"printed {document}";
}

/// <summary>The all-in-one machine signs for all three roles.</summary>
public sealed class AllInOne : IPrinter, IScanner, IFax
{
    public string Print(string document) => $"printed {document}";

    public string Scan(string page) => $"scanned {page}";

    public string Fax(string document, string number) => $"faxed {document} to {number}";
}

/// <summary>Each client asks for the one role it needs, so any printer will do.</summary>
public static class Reception
{
    public static string PrintVisitorBadge(IPrinter printer, string name)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return printer.Print($"badge for {name}");
    }
}
