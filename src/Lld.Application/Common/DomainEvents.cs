using Lld.Application.Abstractions;
using Lld.Domain.Common;

namespace Lld.Application.Common;

/// <summary>Scoped collector for events raised by use cases (one instance per request / command).</summary>
public sealed class DomainEventCollector : IDomainEvents
{
    private readonly List<IDomainEvent> _events = [];

    public void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);

    /// <summary>Returns and clears the collected events.</summary>
    public IReadOnlyList<IDomainEvent> Drain()
    {
        var drained = _events.ToArray();
        _events.Clear();
        return drained;
    }
}

/// <summary>Observer pattern: hands every event to every registered handler.</summary>
public sealed class DomainEventDispatcher(IEnumerable<IDomainEventObserver> handlers)
{
    public void Dispatch(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            foreach (var handler in handlers)
            {
                handler.Handle(domainEvent);
            }
        }
    }
}
