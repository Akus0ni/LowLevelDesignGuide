---
title: "Cheat sheet: Visitor"
---

# Cheat sheet: Visitor

> [!REMEMBER]
> New jobs, same classes. The tax inspector visits every kind of shop; the shops don't learn tax law.

## Use it when

- The classes are stable; new operations keep arriving.
- Those operations don't belong in the classes.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Visitor/After/Shapes.cs#Circle.Accept,SvgExport"
```

## Modern C#

Sealed records + one `switch` per operation; Roslyn walkers and `ExpressionVisitor` for big trees.

## Don't confuse with

- **Iterator:** walks; visitor does the work at each node.
- **Strategy:** one algorithm for one object; visitor is per type.

## Interview one-liners

- "Double dispatch: element type + visitor type."
- "Easy new operations, hard new classes."
