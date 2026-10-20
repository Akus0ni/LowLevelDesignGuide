namespace Lld.Samples.Patterns.Structural.Decorator.After;

/// <summary>The component: what every sender, plain or wrapped, can do.</summary>
public interface IMessageSender
{
    void Send(string to, string text);
}

public sealed class EmailSender : IMessageSender
{
    public List<string> Delivered { get; } = [];

    public void Send(string to, string text) => Delivered.Add($"{to}: {text}");
}

/// <summary>A decorator: is an <see cref="IMessageSender"/>, wraps one, and adds logging around the call.</summary>
public sealed class LoggingSender(IMessageSender inner, List<string> log) : IMessageSender
{
    public void Send(string to, string text)
    {
        log.Add($"sending to {to}");
        inner.Send(to, text);
    }
}

/// <summary>Another decorator: tries the wrapped sender again when it fails.</summary>
public sealed class RetrySender(IMessageSender inner, List<string> log, int attempts = 3) : IMessageSender
{
    public void Send(string to, string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                inner.Send(to, text);
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                log.Add($"retry {attempt}");
            }
        }
    }
}

public static class Senders
{
    /// <summary>Combinations are built, not inherited: stack any decorators in any order.</summary>
    public static IMessageSender Production(EmailSender email, List<string> log) =>
        new LoggingSender(new RetrySender(email, log), log);
}
