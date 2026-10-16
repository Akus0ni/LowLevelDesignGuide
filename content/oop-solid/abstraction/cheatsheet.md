---
title: "Cheat sheet: Abstraction"
---

# Cheat sheet: Abstraction

> [!REMEMBER]
> Show the buttons. Hide the wires.

## Smells

- Interfaces mentioning `FileStream`, folders, `SqlConnection`, `IQueryable`.
- "How" names (`SaveToSqlServer`); methods that need "and" to describe.

## The move

Design the interface from the **caller's** side.

```csharp snippet="samples/Lld.Samples.OopSolid/Abstraction/After/DocumentStore.cs#IDocumentStore"
```

## In this app

`IContentFiles` (disk or memory), `ITokenIssuer`, `TimeProvider`, web `DataSource`.

## Traps

- `IEnumerable<T>` may re-run its query: return `IReadOnlyList<T>`.
- Wrong abstraction > duplication in cost: wait for the 2nd–3rd case.

## Interview one-liners

- "Abstraction decides what you see; encapsulation stops you getting around it."
- "Swap the wires, callers don't change."
