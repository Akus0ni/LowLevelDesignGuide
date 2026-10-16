namespace Lld.Samples.OopSolid.DryKissYagni.After;

/// <summary>KISS: the business has one rule, so the code has one method. Add an engine when there is a second rule.</summary>
public static class MemberDiscount
{
    private const decimal Rate = 0.10m;

    public static decimal Apply(decimal total, bool isMember) => isMember ? total * (1 - Rate) : total;
}

/// <summary>YAGNI: exactly what was asked for. Excel can be a new class the day someone asks.</summary>
public static class CsvReport
{
    public static string Export(IReadOnlyList<string> rows) => string.Join('\n', rows ?? []);
}
