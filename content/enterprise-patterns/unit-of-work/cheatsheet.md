---
title: "Cheat sheet: Unit of Work"
---

# Cheat sheet: Unit of Work

> [!REMEMBER]
> Fill the cart, pay once. Nothing leaves the shop until the whole basket is paid.

## Use it when

- One operation changes several things that must agree.
- Repositories save separately and failures leave half-written data.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Enterprise/UnitOfWork/After/PlaceOrder.cs#UnitOfWork"
```

## Modern C#

`DbContext` + `SaveChangesAsync`, registered scoped: one per request.

## Don't confuse with

- **Repository:** finds and adds; doesn't commit.
- **Transaction:** the mechanism; the pattern also tracks changes.

## Interview one-liners

- "Track changes, commit once, all or nothing."
- "Repositories never call SaveChanges."
