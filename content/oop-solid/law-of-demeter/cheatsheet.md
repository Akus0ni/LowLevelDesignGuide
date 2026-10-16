---
title: "Cheat sheet: Law of Demeter"
---

# Cheat sheet: Law of Demeter

> [!REMEMBER]
> Don't reach into your friend's pocket. Ask your friend.

## Talk only to

Yourself · your fields · your parameters · objects you create.

## Smells

- Train wrecks: `order.Customer.Address.City.Name`.
- Mocks returning mocks.

## The move

Ask the friend; let them decide.

```csharp snippet="samples/Lld.Samples.OopSolid/LawOfDemeter/After/Paperboy.cs#Customer.TryPay"
```

## Not violations

LINQ / fluent builders, DTOs, value objects.

## Interview one-liners

- "It's about who you talk to, not counting dots."
- "Encapsulation, seen from the caller."
