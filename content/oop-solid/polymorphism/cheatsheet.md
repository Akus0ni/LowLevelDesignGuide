---
title: "Cheat sheet: Polymorphism"
---

# Cheat sheet: Polymorphism

> [!REMEMBER]
> Same button. Each thing plays its own way.

## Three kinds

**Subtype** (virtual, interfaces; runtime) · **parametric** (generics) · **ad hoc** (overloads; compile time).

## Smells

- The same `switch` on a type code in many places.
- `is`/`as` checks before acting; `default: throw`.

## The move

One interface, one class per kind.

```csharp snippet="samples/Lld.Samples.OopSolid/Polymorphism/After/Shipping.cs#IShippingMethod"
```

## Types vs operations

More new **types** → polymorphism. More new **operations** on a closed set → records + `switch`.

## In this app

`IAccessRequestState` (State pattern), `ContentValidator` over `IContentRule`, `LinkRewriter.Route` (switch).

## Interview one-liners

- "New kind = new class, not an edit to every switch."
- "Overloads are picked by declared type."
