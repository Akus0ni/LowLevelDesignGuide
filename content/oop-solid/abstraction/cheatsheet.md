---
title: "Cheat sheet: Abstraction"
---

# Cheat sheet: Abstraction

## In one line

Show callers **what** something does, in their terms; hide **how**. Test: the implementation can change completely
and callers don't notice.

## Two places it lives

**Types** (an interface or public surface) and **methods** (one level of detail each; the public method reads as the
business steps).

## Smells

- A method you can't describe without "and"; flag arguments.
- Interfaces that mention `FileStream`, folders, `SqlConnection`, `IQueryable`.
- How-names (`SaveToSqlServer`); callers passing paths and connection strings.

## The move

Design the interface from the **caller's** side, then implement it.

```csharp snippet="samples/Lld.Samples.OopSolid/Abstraction/After/DocumentStore.cs#IDocumentStore"
```

## In this app

`IContentFiles` (repo-relative `/` paths; disk or memory), `ContentPipeline.Build`, `ITokenIssuer`, `TimeProvider`.

## Traps and trade-offs

- `IEnumerable<T>` can re-run its query; return `IReadOnlyList<T>` for results.
- The wrong abstraction costs more than duplication: wait for the 2nd–3rd case.

## Interview one-liners

- "Abstraction decides what you see; encapsulation stops you getting around it."
- "Design the interface for the hardest realistic implementation."
