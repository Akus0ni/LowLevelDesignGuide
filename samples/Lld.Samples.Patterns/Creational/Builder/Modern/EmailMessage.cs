namespace Lld.Samples.Patterns.Creational.Builder.Modern;

/// <summary>
/// Modern C#: <c>required</c> and <c>init</c> give named, optional parts without a builder class. The compiler refuses
/// an e-mail without <see cref="To"/> or <see cref="Subject"/>.
/// </summary>
public sealed record EmailMessage
{
    public required string To { get; init; }

    public required string Subject { get; init; }

    public string Body { get; init; } = string.Empty;

    public bool IsHtml { get; init; }

    public bool HighPriority { get; init; }

    public IReadOnlyList<string> Attachments { get; init; } = [];
}

public static class Emails
{
    public static EmailMessage Welcome(string to) => new() { To = to, Subject = "Welcome", Body = "<h1>Hi!</h1>", IsHtml = true };
}
