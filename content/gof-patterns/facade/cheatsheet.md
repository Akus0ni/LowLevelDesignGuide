---
title: "Cheat sheet: Facade"
---

# Cheat sheet: Facade

> [!REMEMBER]
> One front desk for the whole hotel. Ask the desk; it talks to housekeeping, kitchen and taxi for you.

## Use it when

- Clients repeat the same steps across several classes.
- You want one stable entry point to a changing subsystem.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Facade/After/OrderFacade.cs#OrderFacade.PlaceOrder"
```

## Modern C#

Application services per use case; `AddXyz()` registration methods are facades for start-up.

## Don't confuse with

- **Adapter:** one class, existing interface.
- **Mediator:** objects talk to each other through it (two-way).

## Interview one-liners

- "Simplifies, doesn't hide: the subsystem stays usable."
- "Watch for the god facade."
