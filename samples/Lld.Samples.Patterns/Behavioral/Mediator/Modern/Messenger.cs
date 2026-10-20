namespace Lld.Samples.Patterns.Behavioral.Mediator.Modern;

/// <summary>
/// The MVVM way: view models don't hold each other, they send messages through a messenger (CommunityToolkit's
/// <c>WeakReferenceMessenger</c> works like this small one). The messenger is a general mediator; the rules live in
/// whoever registers for a message.
/// </summary>
public sealed class Messenger
{
    private readonly Dictionary<Type, List<Action<object>>> _handlers = [];

    public void Register<TMessage>(Action<TMessage> handler)
        where TMessage : notnull
    {
        if (!_handlers.TryGetValue(typeof(TMessage), out var list))
        {
            list = [];
            _handlers[typeof(TMessage)] = list;
        }

        list.Add(message => handler((TMessage)message));
    }

    public void Send<TMessage>(TMessage message)
        where TMessage : notnull
    {
        foreach (var handler in _handlers.GetValueOrDefault(typeof(TMessage)) ?? [])
        {
            handler(message);
        }
    }
}

public sealed record CountryChanged(string Country);

public sealed record PostcodeTyped(string Postcode);
