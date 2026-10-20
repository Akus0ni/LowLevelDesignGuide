namespace Lld.Samples.Patterns.Structural.Bridge.Before;

/// <summary>
/// Two things vary (what kind of alert, and how it is sent), but there is one class per pair. Two kinds × two channels
/// is four classes; add push notifications and a "digest" alert and it is nine.
/// </summary>
public abstract class Alert
{
    public abstract string Send(string text);
}

public sealed class UrgentEmailAlert : Alert
{
    public override string Send(string text) => $"email: URGENT {text}";
}

public sealed class UrgentSmsAlert : Alert
{
    public override string Send(string text) => $"sms: URGENT {text}";
}

public sealed class ReminderEmailAlert : Alert
{
    public override string Send(string text) => $"email: reminder: {text}";
}

public sealed class ReminderSmsAlert : Alert
{
    public override string Send(string text) => $"sms: reminder: {text}";
}
