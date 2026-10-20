---
title: "Cheat sheet: Observer"
---

# Cheat sheet: Observer

> [!REMEMBER]
> Subscribe once, hear every change. The channel doesn't know its subscribers; it just rings the bell.

## Use it when

- One change must trigger work in several places.
- New reactions keep arriving; the source shouldn't change.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Observer/After/OrderService.cs#OrderService.Place"
```

## Modern C#

`event EventHandler<T>`, `IObservable<T>` (Rx), `Channel<T>`; a broker across services.

## Don't confuse with

- **Pub/sub:** a broker in between; nobody knows anybody.
- **Mediator:** coordinates two-way talk, not just broadcasts.

## Interview one-liners

- "One-to-many, subject doesn't know concrete observers."
- "Unsubscribe, or you leak (lapsed listener)."
