namespace Lld.Samples.Patterns.Creational.Prototype.Before;

public sealed class Quote
{
    public string Title { get; set; } = string.Empty;

    public string Currency { get; set; } = "INR";

    public List<string> Lines { get; init; } = [];
}

/// <summary>
/// Every caller copies the template by hand. This copy forgot that <see cref="Quote.Lines"/> is a list, so the new
/// quote and the template share it.
/// </summary>
public static class Quotes
{
    public static Quote CopyOf(Quote template) => new()
    {
        Title = template.Title,
        Currency = template.Currency,
        Lines = template.Lines,
    };
}
