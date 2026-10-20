namespace Lld.Samples.Patterns.Enterprise.DiVsServiceLocator.After;

/// <summary>
/// Dependency injection: what the class needs is in its constructor. It can't be created without a mailer, and a test
/// just passes one in.
/// </summary>
public sealed class InvoiceSender(IMailer mailer)
{
    public string Message { get; init; } = "Your invoice is ready.";

    public void Send(string customer) => mailer.Send(customer, Message);
}
