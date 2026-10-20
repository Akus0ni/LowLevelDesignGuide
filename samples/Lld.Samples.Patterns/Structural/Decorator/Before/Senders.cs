namespace Lld.Samples.Patterns.Structural.Decorator.Before;

/// <summary>
/// Extra behaviour by subclassing: one class per combination. Logging, retrying, and both: three classes already;
/// add "rate limit" and there are seven. The retry and logging code is copied between them.
/// </summary>
public class EmailSender
{
    public List<string> Delivered { get; } = [];

    public virtual void Send(string to, string text) => Delivered.Add($"{to}: {text}");
}

public class LoggingEmailSender : EmailSender
{
    public List<string> Log { get; } = [];

    public override void Send(string to, string text)
    {
        Log.Add($"sending to {to}");
        base.Send(to, text);
    }
}

public class RetryingLoggingEmailSender : LoggingEmailSender
{
    public override void Send(string to, string text)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                base.Send(to, text);
                return;
            }
            catch (IOException) when (attempt < 3)
            {
                Log.Add($"retry {attempt}");
            }
        }
    }
}
