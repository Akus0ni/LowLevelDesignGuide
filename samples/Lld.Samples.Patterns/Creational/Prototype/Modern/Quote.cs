using System.Collections.Immutable;

namespace Lld.Samples.Patterns.Creational.Prototype.Modern;

/// <summary>
/// Modern C#: a record copies itself with <c>with</c>. Its parts are immutable, so sharing them is safe and a
/// shallow copy is enough.
/// </summary>
public sealed record Quote(string Title, string Currency, ImmutableList<string> Lines);

public static class Quotes
{
    public static Quote ForMonth(Quote template, string month) => template with { Title = $"{template.Title}: {month}" };
}
