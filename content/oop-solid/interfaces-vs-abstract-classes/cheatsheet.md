---
title: "Cheat sheet: Interfaces vs abstract classes"
---

# Cheat sheet: Interfaces vs abstract classes

> [!REMEMBER]
> Interface = a job ad: "can pay". Abstract class = a half-built house: shared walls, finish the rest.

## Pick

- **Interface:** a capability, many unrelated types, test fakes.
- **Abstract class:** shared state or code in one close family; Template Method.

## The move

Callers use the interface; a family shares an abstract base behind it.

```csharp snippet="samples/Lld.Samples.OopSolid/InterfacesVsAbstractClasses/After/Payments.cs#IPaymentMethod"
```

## In this app

`ISchemaMigration` (capability) vs `AggregateRoot` (shared state).

## C#

Default interface methods grow interfaces safely; static abstract members (`T.Zero`) power generic code.

## Interview one-liners

- "Many interfaces, one base class: spend the base class on real shared code."
