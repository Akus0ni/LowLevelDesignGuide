---
title: "Cheat sheet: Value objects & immutability"
---

# Cheat sheet: Value objects & immutability

> [!REMEMBER]
> A ₹100 note is a ₹100 note. You don't care which one.

## Value vs entity

**Value:** defined by values, equal by value, never changes. **Entity:** defined by identity.

## Smells

- Primitive obsession: `string email`, `decimal amount, string currency`.
- Copied validation; easy-to-swap same-type parameters.

## The move

A small type that validates once and never changes.

```csharp snippet="samples/Lld.Samples.OopSolid/ValueObjects/After/Money.cs#Money.Money"
```

## In this app

`EmailAddress.Create`: trimmed, lower-cased, checked once. `Error`, `ContentIssue`.

## Interview one-liners

- "Entities have identity; values have equality."
- "Valid from birth, never changed: safe to share."
