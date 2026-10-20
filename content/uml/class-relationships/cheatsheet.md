---
title: "Cheat sheet: Relationships: the six arrows"
---

# Cheat sheet: Relationships: the six arrows

> [!REMEMBER]
> **Solid** line: it holds one. **Dashed**: it uses or implements one. **Triangle**: is a. **Diamond**: owns (filled =
> the parts die with it).

## Draw it when

Ownership, lifetimes or an interface between classes matter to the design.

## The symbols

```uml-arrows
```

## From C#

```csharp snippet="samples/Lld.Samples.Uml/Relationships/Shop.cs#Order"
```

`List<OrderLine>` it fills itself → `Order *-- OrderLine`; `Total(TaxCalculator tax)` → `Order ..> TaxCalculator`.

## Mistakes

Inheritance arrow pointing at the child; composition for shared parts; a two-way line by default.

## Interview one-liners

- "Composition: the parts die with the whole. Aggregation: they don't."
- "Dashed means uses or implements; solid means holds."
- "Arrows point from the one that knows to the one it knows."
