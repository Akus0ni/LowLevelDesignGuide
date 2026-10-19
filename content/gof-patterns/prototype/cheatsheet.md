---
title: "Cheat sheet: Prototype"
---

# Cheat sheet: Prototype

> [!REMEMBER]
> Copy the cookie, don't bake again. The object clones itself, so it knows what must be deep-copied.

## Use it when

- Setting up an object is costly; new ones differ a little.
- Hand-made copies forget a list (shared reference).

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Creational/Prototype/After/Quote.cs#Quote.Clone"
```

## Modern C#

`template with { Title = "March" }` on a record with immutable parts.

## Don't confuse with

- **`MemberwiseClone`:** shallow only.
- **`ICloneable`:** says nothing about depth; avoid in APIs.

## Interview one-liners

- "Shallow copies share references; know which one you make."
- "Records + immutable parts = safe shallow copies."
