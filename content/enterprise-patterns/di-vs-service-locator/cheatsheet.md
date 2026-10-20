---
title: "Cheat sheet: DI vs Service Locator"
---

# Cheat sheet: DI vs Service Locator

> [!REMEMBER]
> Served at the table, not fetched. The waiter brings what you ordered; you don't wander into the kitchen.

## Use it when

- Always for business code: dependencies in the constructor.
- The container only at the composition root.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Enterprise/DiVsServiceLocator/After/InvoiceSender.cs#InvoiceSender"
```

## Modern C#

`AddScoped` / `AddSingleton` / `AddTransient`; factories (`Func<T>`, `IDbContextFactory<T>`) instead of `IServiceProvider`.

## Don't confuse with

- **DIP:** a principle; DI is a technique.
- **Container:** automates DI; not required for it.

## Interview one-liners

- "Locator hides dependencies; DI shows them."
- "Big constructor = SRP problem, not a DI problem."
