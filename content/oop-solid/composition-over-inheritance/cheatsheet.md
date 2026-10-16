---
title: "Cheat sheet: Composition over inheritance"
---

# Cheat sheet: Composition over inheritance

> [!REMEMBER]
> Build with LEGO bricks, not family trees.

## Smells

- Class names listing features (`TimestampedUpperCaseLogger`).
- Inheriting a big class for one method; "I need two base classes".

## The move

Small interface; each feature **has** one and passes calls on (Decorator).

```csharp snippet="samples/Lld.Samples.OopSolid/CompositionOverInheritance/After/Loggers.cs#Timestamped"
```

## Strategy vs Decorator

**Strategy:** pick one way. **Decorator:** wrap to add, stack many.

## In this app

`ContentPipeline` has a reader, validator and packer; tests swap in `InMemoryContentFiles`.

## Interview one-liners

- "Has-a, swap at runtime; is-a, fixed at compile time."
- "Three features: three bricks, not seven subclasses."
