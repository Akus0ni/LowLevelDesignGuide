---
title: "Cheat sheet: DRY, KISS & YAGNI"
---

# Cheat sheet: DRY, KISS & YAGNI

> [!REMEMBER]
> Say it once. Keep it simple. Build only what's asked.

## Smells

- **DRY:** the same rule in several places.
- **KISS:** engines and factories for one case.
- **YAGNI:** `NotImplementedException` "for later".

## The move

One source of truth for each rule.

```csharp snippet="samples/Lld.Samples.OopSolid/DryKissYagni/After/Shipping.cs#ShippingPolicy"
```

## Rule of three

Write it, then copy it, then extract it on the third time.

## In this app

Plain SQL schema steps (KISS), one shared slug test file for C# and TypeScript (DRY).

## Interview one-liners

- "DRY is about knowledge, not lines."
- "Duplication is cheaper than the wrong abstraction."
