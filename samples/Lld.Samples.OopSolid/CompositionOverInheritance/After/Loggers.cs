namespace Lld.Samples.OopSolid.CompositionOverInheritance.After;

/// <summary>One small brick: something that writes a line.</summary>
public interface ILog
{
    void Log(string message);
}

/// <summary>Where lines end up. Swap it for a file or a console without touching anything else.</summary>
public sealed class MemoryLog : ILog
{
    public List<string> Written { get; } = [];

    public void Log(string message) => Written.Add(message);
}

/// <summary>Adds a time, then hands the line on. It has a log; it isn't one.</summary>
public sealed class Timestamped(ILog inner, TimeProvider clock) : ILog
{
    public void Log(string message) => inner.Log($"[{clock.GetLocalNow():HH:mm}] {message}");
}

public sealed class UpperCase(ILog inner) : ILog
{
    public void Log(string message) => inner.Log(message.ToUpperInvariant());
}
