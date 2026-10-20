namespace Lld.Samples.Patterns.Enterprise.DiVsServiceLocator.Before;

/// <summary>A global registry anyone can ask for anything.</summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = [];

    public static void Register<T>(T service)
        where T : notnull => Services[typeof(T)] = service;

    public static T Get<T>() =>
        Services.TryGetValue(typeof(T), out var service)
            ? (T)service
            : throw new InvalidOperationException($"{typeof(T).Name} was never registered.");
}

/// <summary>
/// The constructor says "I need nothing", but <see cref="Send"/> secretly needs a mailer. You only find out at
/// runtime, and every test must set up the global registry (and can leak it into the next test).
/// </summary>
public sealed class InvoiceSender
{
    public string Message { get; init; } = "Your invoice is ready.";

    public void Send(string customer) => ServiceLocator.Get<IMailer>().Send(customer, Message);
}
