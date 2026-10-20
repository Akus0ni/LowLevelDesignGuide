namespace Lld.Samples.Patterns.Behavioral.Observer;

/// <summary>Three things that care when an order is placed. Each records what it did. Shared by every version.</summary>
public sealed class Mailer
{
    public List<string> Sent { get; } = [];

    public void Send(string text) => Sent.Add(text);
}

public sealed class Sms
{
    public List<string> Sent { get; } = [];

    public void Send(string text) => Sent.Add(text);
}

public sealed class Analytics
{
    public List<string> Tracked { get; } = [];

    public void Track(string name) => Tracked.Add(name);
}
