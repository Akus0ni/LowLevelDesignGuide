---
title: "Cheat sheet: Abstract Factory"
---

# Cheat sheet: Abstract Factory

> [!REMEMBER]
> One showroom, everything matches. Pick the family once; every piece comes from it.

## Use it when

- Products come in families that must not mix.
- `if (theme == ...)` is spread across many classes.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Creational/AbstractFactory/After/Themes.cs#IThemeFactory,DarkTheme"
```

## Modern C#

Register one family in DI at start-up (`AddSingleton<IThemeFactory, DarkTheme>()`); keyed services for several.

## Don't confuse with

- **Factory Method:** one product, chosen by a subclass.
- **Builder:** one product assembled step by step.

## Interview one-liners

- "New family: easy. New product type: touches every factory."
- "`DbProviderFactory` is the .NET example."
