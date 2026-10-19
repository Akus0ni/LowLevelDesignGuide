---
title: "Cheat sheet: Builder"
---

# Cheat sheet: Builder

> [!REMEMBER]
> Step by step, check at the end. Name each part; `Build()` hands over the finished thing.

## Use it when

- Many parameters, several optional or of the same type.
- The object must be valid as a whole, never half-built.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Creational/Builder/After/EmailBuilder.cs#Emails"
```

## Modern C#

`required` + `init` properties for simple cases; keep a builder for steps and cross-field rules.

## Don't confuse with

- **Abstract Factory:** a family, one call each.
- **Fluent interface:** a style; Builder is about one checked product.

## Interview one-liners

- "Kills telescoping constructors."
- "`WebApplication.CreateBuilder` → `Build()` is a builder."
