---
title: "Cheat sheet: Bridge"
---

# Cheat sheet: Bridge

> [!REMEMBER]
> Split what from how; vary both. One remote works any TV; any TV works with a new remote.

## Use it when

- Class names combine two ideas and grow as m × n.
- The implementation should be chosen or swapped at runtime.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Bridge/After/Alerts.cs#Alert"
```

## Modern C#

DI (keyed services) pairs each kind with an implementation.

## Don't confuse with

- **Adapter:** added later to fix a mismatch.
- **Strategy:** swaps one algorithm; same shape, different intent.

## Interview one-liners

- "m × n classes become m + n."
- "Inherit along one dimension, compose along the other."
