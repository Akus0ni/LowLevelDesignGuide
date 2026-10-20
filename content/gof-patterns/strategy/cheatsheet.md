---
title: "Cheat sheet: Strategy"
---

# Cheat sheet: Strategy

> [!REMEMBER]
> Swap how, keep what. Same trip, pick car, bike or walk.

## Use it when

- A growing `switch` picks between versions of one calculation.
- Behaviour is chosen at runtime or tested alone.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Strategy/After/Shipping.cs#IShippingCost,Checkout"
```

## Modern C#

`Func<Order, decimal>` for one method; keyed DI services for named strategies.

## Don't confuse with

- **State:** the object swaps itself; strategy is chosen by the caller.
- **Template Method:** varies steps by inheritance.

## Interview one-liners

- "A family of algorithms, interchangeable, chosen from outside."
- "OCP: new behaviour is a new class."
