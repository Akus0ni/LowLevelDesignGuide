---
title: "Cheat sheet: Polymorphism"
---

# Cheat sheet: Polymorphism

## In one line

One call, many behaviours: code written against a general type works with every specific one, chosen at runtime.

## Three kinds

- **Subtype:** `virtual` / interfaces, runtime dispatch.
- **Parametric:** generics (`Largest<T> where T : IComparable<T>`).
- **Ad hoc:** overloads, operators (compile time!).

## Smells

- The same `switch` on a type code in several places.
- `is`/`as` checks before acting; flags that pick behaviour.
- `default: throw "unknown type"`.

## The move

Replace the type code: one interface, one class per value, each answers for itself.

```csharp snippet="samples/Lld.Samples.OopSolid/Polymorphism/After/Shipping.cs#IShippingMethod"
```

## Types vs operations

More new **types** → polymorphism. More new **operations** on a closed set → records + switch expression
(the expression problem).

## In this app

`IAccessRequestState` (State pattern), `ContentValidator` looping over `IContentRule`, `LinkRewriter.Route`
switching on the closed `LinkTarget` family, `Result<T>`.

## Interview one-liners

- "New variant = new class, not an edit to every switch."
- "Overloads are picked by the declared type, not the runtime type."
