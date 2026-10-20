namespace Lld.Samples.Patterns.Enterprise.DiVsServiceLocator;

public interface IMailer
{
    void Send(string to, string text);
}

/// <summary>Records what was sent instead of sending. Shared by every version.</summary>
public sealed class OutboxMailer : IMailer
{
    public List<string> Sent { get; } = [];

    public void Send(string to, string text) => Sent.Add($"{to}: {text}");
}
