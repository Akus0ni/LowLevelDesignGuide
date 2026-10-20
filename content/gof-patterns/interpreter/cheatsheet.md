---
title: "Cheat sheet: Interpreter"
---

# Cheat sheet: Interpreter

> [!REMEMBER]
> Each word knows what it means. Sheet music: every note and rest knows how to be played; the song is the tree.

## Use it when

- Users write simple rules or filters; code picks strings apart.
- The language is small and stable.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Interpreter/After/Rules.cs#IRule,And"
```

## Modern C#

`System.Linq.Expressions` trees: `Compile()` to a delegate, or let EF Core translate to SQL.

## Don't confuse with

- **Composite:** the tree shape; interpreter adds evaluation.
- **Visitor:** adds operations to the tree from outside.

## Interview one-liners

- "One class per grammar rule; sentence = tree."
- "Parsing is a separate job."
