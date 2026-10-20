namespace Lld.Samples.Patterns.Creational.Prototype.After;

/// <summary>The prototype copies itself, so the one class that knows its parts decides what must be deep-copied.</summary>
public sealed class Quote
{
    public string Title { get; set; } = string.Empty;

    public string Currency { get; set; } = "INR";

    public List<string> Lines { get; init; } = [];

    public Quote Clone() => new() { Title = Title, Currency = Currency, Lines = [.. Lines] };
}

/// <summary>A registry of ready-made prototypes: a new quote starts as a copy of a configured one.</summary>
public sealed class QuoteTemplates
{
    private readonly Dictionary<string, Quote> _templates = new(StringComparer.Ordinal);

    public void Add(string name, Quote template) => _templates[name] = template;

    public Quote New(string name) => _templates[name].Clone();
}
