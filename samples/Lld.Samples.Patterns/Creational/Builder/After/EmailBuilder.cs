namespace Lld.Samples.Patterns.Creational.Builder.After;

/// <summary>The product: finished and read-only. Only the builder can make one.</summary>
public sealed class EmailMessage
{
    internal EmailMessage(string to, string subject, string body, bool isHtml, bool highPriority, IReadOnlyList<string> attachments)
    {
        To = to;
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
        HighPriority = highPriority;
        Attachments = attachments;
    }

    public string To { get; }

    public string Subject { get; }

    public string Body { get; }

    public bool IsHtml { get; }

    public bool HighPriority { get; }

    public IReadOnlyList<string> Attachments { get; }
}

/// <summary>The builder: one named step per part, in any order; <see cref="Build"/> checks the whole thing once.</summary>
public sealed class EmailBuilder
{
    private readonly List<string> _attachments = [];
    private string? _to;
    private string _subject = string.Empty;
    private string _body = string.Empty;
    private bool _isHtml;
    private bool _highPriority;

    public EmailBuilder To(string address)
    {
        _to = address;
        return this;
    }

    public EmailBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    public EmailBuilder HtmlBody(string html)
    {
        _body = html;
        _isHtml = true;
        return this;
    }

    public EmailBuilder HighPriority()
    {
        _highPriority = true;
        return this;
    }

    public EmailBuilder Attach(string file)
    {
        _attachments.Add(file);
        return this;
    }

    public EmailMessage Build() =>
        _to is null
            ? throw new InvalidOperationException("An e-mail needs a recipient: call To(...).")
            : new EmailMessage(_to, _subject, _body, _isHtml, _highPriority, [.. _attachments]);
}

public static class Emails
{
    public static EmailMessage Welcome(string to) =>
        new EmailBuilder().To(to).Subject("Welcome").HtmlBody("<h1>Hi!</h1>").Build();
}
