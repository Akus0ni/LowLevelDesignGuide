namespace Lld.Samples.OopSolid.CompositionOverInheritance.Before;

/// <summary>Every combination of "where" and "how" needs its own subclass.</summary>
public class Logger
{
    public List<string> Written { get; } = [];

    public virtual void Log(string message) => Written.Add(message);
}

public class TimestampedLogger : Logger
{
    public override void Log(string message) => base.Log($"[09:00] {message}");
}

public class UpperCaseLogger : Logger
{
    public override void Log(string message) => base.Log(message.ToUpperInvariant());
}

/// <summary>Both? Pick a parent, copy the other one's code. Three features would need seven classes.</summary>
public sealed class TimestampedUpperCaseLogger : TimestampedLogger
{
    public override void Log(string message) => base.Log(message.ToUpperInvariant());
}
