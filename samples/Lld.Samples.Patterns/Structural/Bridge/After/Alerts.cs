namespace Lld.Samples.Patterns.Structural.Bridge.After;

/// <summary>The implementation side: how a message travels.</summary>
public interface IChannel
{
    string Deliver(string message);
}

public sealed class EmailChannel : IChannel
{
    public string Deliver(string message) => $"email: {message}";
}

public sealed class SmsChannel : IChannel
{
    public string Deliver(string message) => $"sms: {message}";
}

/// <summary>
/// The abstraction side: what kind of alert it is. It holds a channel (the bridge) instead of inheriting one, so
/// kinds and channels grow separately: 2 + 2 classes, not 2 × 2.
/// </summary>
public abstract class Alert(IChannel channel)
{
    public string Send(string text) => channel.Deliver(Format(text));

    protected abstract string Format(string text);
}

public sealed class UrgentAlert(IChannel channel) : Alert(channel)
{
    protected override string Format(string text) => $"URGENT {text}";
}

public sealed class ReminderAlert(IChannel channel) : Alert(channel)
{
    protected override string Format(string text) => $"reminder: {text}";
}
