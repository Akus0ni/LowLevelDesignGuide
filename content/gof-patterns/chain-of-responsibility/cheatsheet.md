---
title: "Cheat sheet: Chain of Responsibility"
---

# Cheat sheet: Chain of Responsibility

> [!REMEMBER]
> Pass it on until someone can. Support goes bot, then agent, then manager.

## Use it when

- A long `if / else if` decides who handles a request.
- Several steps may look at a request and any may stop it.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/ChainOfResponsibility/After/Approvers.cs#ExpenseHandler"
```

## Modern C#

Delegate pipelines, exactly like ASP.NET Core middleware (`next`).

## Don't confuse with

- **Decorator:** always passes on; a chain may stop.
- **Observer:** everyone hears it; a chain stops at the handler.

## Interview one-liners

- "Sender doesn't know the receiver."
- "Decide what happens at the end of the chain."
