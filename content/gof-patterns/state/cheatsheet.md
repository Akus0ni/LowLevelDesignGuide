---
title: "Cheat sheet: State"
---

# Cheat sheet: State

> [!REMEMBER]
> Behaviour follows the state. The same "go" button does nothing at red and drives at green.

## Use it when

- The same `switch (status)` appears in many methods.
- State rules are scattered and easy to forget.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/State/After/Document.cs#InReview"
```

## Modern C#

Small machines: a `switch` on `(state, action)`; big ones: state classes or the Stateless library.

## Don't confuse with

- **Strategy:** chosen by the caller, rarely changes.
- **Status enum:** data, not behaviour.

## Interview one-liners

- "Each state is a class; the object swaps its state."
- "Store an enum, rebuild the state object on load."
