---
title: "Cheat sheet: Interface Segregation (ISP)"
---

# Cheat sheet: Interface Segregation (ISP)

> [!REMEMBER]
> Don't make me sign for things I don't use.

## Smells

- Implementations that throw `NotSupportedException` or do nothing.
- Big interfaces callers use only a bit of; fakes stubbing unused methods.

## The move

One small **role** interface per client need; a class may play many roles.

```csharp snippet="samples/Lld.Samples.OopSolid/Isp/After/Machines.cs#IPrinter"
```

## In this app

Small ports: `IUnitOfWork`, `IAuditLog`; `IContentCatalog` only reads.

## Interview one-liners

- "One role per interface, not one method."
- "Fat interfaces make implementations lie, which breaks LSP."
