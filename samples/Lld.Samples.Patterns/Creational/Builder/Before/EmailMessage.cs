namespace Lld.Samples.Patterns.Creational.Builder.Before;

/// <summary>
/// One constructor with seven parameters. Which <c>bool</c> is which? Callers pass <c>null</c> for what they don't
/// need, and nothing stops an e-mail without a recipient.
/// </summary>
public sealed class EmailMessage(
    string to, string subject, string body, string? cc, bool isHtml, bool highPriority, IReadOnlyList<string>? attachments)
{
    public string To { get; } = to;

    public string Subject { get; } = subject;

    public string Body { get; } = body;

    public string? Cc { get; } = cc;

    public bool IsHtml { get; } = isHtml;

    public bool HighPriority { get; } = highPriority;

    public IReadOnlyList<string> Attachments { get; } = attachments ?? [];
}

public static class Emails
{
    public static EmailMessage Welcome(string to) => new(to, "Welcome", "<h1>Hi!</h1>", null, true, false, null);
}
