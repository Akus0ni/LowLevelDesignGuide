---
title: "Cheat sheet: Liskov Substitution (LSP)"
---

# Cheat sheet: Liskov Substitution (LSP)

> [!REMEMBER]
> If it looks like a duck but needs batteries, it's the wrong duck.

## Smells

- Overrides that throw `NotSupportedException`.
- `is` checks on the subtype in callers.

## The contract

Don't demand **more** (preconditions), don't deliver **less** (postconditions), keep the **invariants**.

## The move

Siblings behind a smaller interface, plus a contract test.

```csharp snippet="samples/Lld.Samples.OopSolid/Lsp/After/Shapes.cs#IShape"
```

## In this app

`ContentCatalogContract` runs against both catalogs (in-memory, SQLite).

## Interview one-liners

- "Swap it in; if a caller is surprised, it's a violation."
- "Square : Rectangle breaks the 'independent sides' promise."
