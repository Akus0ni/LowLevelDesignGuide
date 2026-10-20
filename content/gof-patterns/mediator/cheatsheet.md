---
title: "Cheat sheet: Mediator"
---

# Cheat sheet: Mediator

> [!REMEMBER]
> Everyone talks to the tower. Planes never call each other; the tower decides who lands.

## Use it when

- Objects hold many others just to keep them in sync.
- Rules between objects are spread everywhere.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Mediator/After/AddressForm.cs#AddressForm.Changed"
```

## Modern C#

MVVM messengers (`WeakReferenceMessenger`); MediatR is a request dispatcher, not the GoF mediator.

## Don't confuse with

- **Observer:** one subject broadcasts; mediator coordinates both ways.
- **Facade:** one-way front door; subsystem doesn't know it.

## Interview one-liners

- "Many-to-many becomes many-to-one."
- "Watch the god mediator."
