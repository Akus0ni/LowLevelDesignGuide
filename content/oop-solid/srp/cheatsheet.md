---
title: "Cheat sheet: Single Responsibility (SRP)"
---

# Cheat sheet: Single Responsibility (SRP)

> [!REMEMBER]
> One class, one boss. Only one person should ever ask you to change it.

## Smells

- Describing it needs "and"; `Manager` / `Processor` / `Helper`.
- Unrelated changes keep landing in one file.

## The move

Group methods by **who asks for changes**; one class per group.

```csharp snippet="samples/Lld.Samples.OopSolid/Srp/After/Invoice.cs#InvoiceHtml"
```

## In this app

One class per auth use case (`RejectAccessRequest`); reader / validator / packer.

## Interview one-liners

- "One reason to change = one actor."
- "Split by who changes it, not by size."
