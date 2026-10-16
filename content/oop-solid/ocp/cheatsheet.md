---
title: "Cheat sheet: Open/Closed (OCP)"
---

# Cheat sheet: Open/Closed (OCP)

> [!REMEMBER]
> Add new code. Don't edit old code. Like a power strip: plug in, no rewiring.

## Smells

- A `switch` / `if` chain that grows with every feature.
- "To add X, edit five places."

## The move

Small interface at the point of change; each variation is a class.

```csharp snippet="samples/Lld.Samples.OopSolid/Ocp/After/Discounts.cs#IDiscountRule"
```

## In this app

`IContentRule` (eleven new rules, validator unchanged); `ISchemaMigration` steps.

## Interview one-liners

- "New requirement = new class, no edits."
- "Extension points only where change has really happened."
