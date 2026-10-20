---
title: "Cheat sheet: Multiplicity and navigability"
---

# Cheat sheet: Multiplicity and navigability

> [!REMEMBER]
> Read the number **at the far end**: "one library holds **0..\*** books". `0..1` is nullable, `1..*` is a list that
> is never empty. The arrowhead says who can find whom.

## Draw it when

"How many?" is a rule, or changes a type; one side must not know the other.

## The symbols

| `1` | `0..1` | `0..*` | `1..*` | `-->` |
|---|---|---|---|---|
| required | nullable | list | non-empty list | one way |

## From C#

```csharp snippet="samples/Lld.Samples.Uml/Multiplicity/Library.cs#Book.Book,Book.Borrower"
```

The constructor refuses no authors (`1..*`); `Member? Borrower` is `0..1`.

## Mistakes

Number read at the wrong end; two-way links by default; `1..*` not enforced.

## Interview one-liners

- "Read the far end."
- "0..1 is nullable, 1..* is checked in the constructor."
- "One-way unless a query needs the way back."
