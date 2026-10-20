---
title: "Cheat sheet: Singleton"
---

# Cheat sheet: Singleton

> [!REMEMBER]
> One for everyone, made once. In .NET: let the container keep it single.

## Use it when

- Many parts must share **one** thing (settings, cache, counter).
- Two of them would be **wrong** or **costly**.

## The shape

Private constructor + static `Instance`, made once with `Lazy<T>`.

```csharp snippet="samples/Lld.Samples.Patterns/Creational/Singleton/After/AppSettings.cs#AppSettings"
```

## Modern C#

`services.AddSingleton<AppSettings>()` and constructor injection: one object, visible dependency, testable.

## Don't confuse with

- **Static class:** no instance, no interface, can't be injected.
- **DI singleton:** same "one", but the container decides, not the class.

## Interview one-liners

- "The problem is global access, not one instance."
- "Singletons must be thread-safe and must not hold scoped services."
