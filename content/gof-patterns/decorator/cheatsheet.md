---
title: "Cheat sheet: Decorator"
---

# Cheat sheet: Decorator

> [!REMEMBER]
> Wrap it to add, layer by layer. Coffee + milk + sugar: still a coffee, every time.

## Use it when

- Combinations of extras multiply subclasses.
- You want to add behaviour at runtime without touching the class.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Decorator/After/Senders.cs#LoggingSender"
```

## Modern C#

`DelegatingHandler` for `HttpClient`; Scrutor's `Decorate<T, TDecorator>()` for DI services.

## Don't confuse with

- **Proxy:** same shape, controls access instead of adding behaviour.
- **Adapter:** changes the interface.

## Interview one-liners

- "Same interface, wraps one, adds one thing."
- "Order of wrappers is a decision: write it down."
