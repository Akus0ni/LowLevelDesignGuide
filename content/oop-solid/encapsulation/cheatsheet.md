---
title: "Cheat sheet: Encapsulation"
---

# Cheat sheet: Encapsulation

> [!REMEMBER]
> Don't touch my data. Ask me to do it.

## Smells

- `{ get; set; }` on data with rules; rules in a `…Service`.
- A `List<T>` handed out; a stored value that could be computed.

## The move

Setters → business actions. One private method changes related state.

```csharp snippet="samples/Lld.Samples.OopSolid/Encapsulation/After/BankAccount.cs#BankAccount.Withdraw,BankAccount.Record"
```

## In this app

`AccessRequest`: only `private set`; `Approve` → state → `internal ApplyDecision`.

## Traps

- `IReadOnlyList<T>` can be cast back; `AsReadOnly()` can't.
- `with` skips the constructor: validate in `init`.

## Interview one-liners

- "Private fields are the tool; protected invariants are the goal."
- "A public setter is a public field with extra typing."
