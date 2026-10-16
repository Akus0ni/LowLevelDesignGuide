---
title: "Cheat sheet: Coupling & cohesion"
---

# Cheat sheet: Coupling & cohesion

> [!REMEMBER]
> Tight inside, loose outside. Like a tidy drawer with one handle.

## Definitions

**Cohesion** (want high): parts inside belong together. **Coupling** (want low): how much you know about others.

## Smells

- `Helpers` / `Utils` / `Manager` grab-bags.
- Deep `a.B.C.D` chains; one change → many files.

## The move

Move code next to the data it uses; depend on a small record, not a whole graph.

```csharp snippet="samples/Lld.Samples.OopSolid/CouplingCohesion/After/Receipts.cs#ReceiptFormatter"
```

## In this app

`Api → Infrastructure → Application → Domain`; the domain depends on nothing.

## Interview one-liners

- "Change one thing, touch one place."
- "SRP is cohesion applied to change."
