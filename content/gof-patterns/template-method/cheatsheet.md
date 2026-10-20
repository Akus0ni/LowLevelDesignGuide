---
title: "Cheat sheet: Template Method"
---

# Cheat sheet: Template Method

> [!REMEMBER]
> Fixed recipe, your own fillings. The sandwich steps never change; you choose what goes inside.

## Use it when

- Classes repeat the same steps with small differences.
- A framework owns the flow; users plug in parts.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/TemplateMethod/After/Reports.cs#Report.Render"
```

## Modern C#

A record of delegates (Strategy) instead of subclasses; `BackgroundService.ExecuteAsync` is a hook.

## Don't confuse with

- **Strategy:** swaps the whole algorithm by composition.
- **Factory Method:** one step that creates an object.

## Interview one-liners

- "Non-virtual skeleton, overridable steps."
- "Hollywood principle: we'll call you."
