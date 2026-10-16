---
title: "Cheat sheet: Encapsulation"
---

# Cheat sheet: Encapsulation

## In one line

An object protects its **invariants**: state changes only through methods that keep its rules true, so each rule
lives in one place.

## Smells

- Public setters on state with rules; rules in a `…Service` (anaemic model).
- A mutable `List<T>` handed out, even without a setter.
- Ask → decide → write back, instead of *tell*; stored values that could be computed.

## The move

Replace setters with business operations; change related state in **one** private method.

```csharp snippet="samples/Lld.Samples.OopSolid/Encapsulation/After/BankAccount.cs#BankAccount.Withdraw,BankAccount.Record"
```

## In this app

`AccessRequest` (`private set`; `Approve` → state → `internal ApplyDecision`), `RefreshToken.Rotate`. Leak:
`AggregateRoot.DomainEvents`.

## C# traps

- `IReadOnlyList<T>` over your list can be cast back; `AsReadOnly()` can't.
- `with` skips the constructor: validate in `init` (C# 14 `field`).
- DTOs, options and messages stay plain: no rules, nothing to protect.

## Interview one-liners

- "Private fields are the mechanism; protected invariants are the goal."
- "A public setter is a public field with extra typing."
- "Tell, don't ask: let the object make decisions about its own data."
