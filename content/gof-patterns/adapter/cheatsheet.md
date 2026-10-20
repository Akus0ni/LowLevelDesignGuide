---
title: "Cheat sheet: Adapter"
---

# Cheat sheet: Adapter

> [!REMEMBER]
> Make it fit, don't rewrite it. A travel plug: the socket and the charger stay as they are.

## Use it when

- A class you must use has the wrong interface, units or data shapes.
- Conversion code is copied wherever it is called.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Adapter/After/Payments.cs#LegacyPayAdapter"
```

## Modern C#

C# 14 `extension` members for a nicer call; the adapter class when callers need an interface.

## Don't confuse with

- **Facade:** a new, simpler interface over many classes.
- **Decorator:** same interface, extra behaviour.

## Interview one-liners

- "Implements our interface, translates to theirs."
- "Infrastructure classes behind ports are adapters."
