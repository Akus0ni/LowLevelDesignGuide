---
title: "Cheat sheet: Null Object"
---

# Cheat sheet: Null Object

> [!REMEMBER]
> Nothing, done safely. A mute button doesn't break the TV; it just plays no sound.

## Use it when

- Null checks for one optional dependency are everywhere.
- "Do nothing" is a correct behaviour.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Enterprise/NullObject/After/Checkout.cs#NoDiscount"
```

## Modern C#

`NullLogger<T>.Instance`, `TextWriter.Null`, `Stream.Null`, `[]` + nullable reference types.

## Don't confuse with

- **Default values:** data, not behaviour.
- **Swallowing errors:** missing required things must fail loudly.

## Interview one-liners

- "A real object that does nothing, instead of null."
- "Return empty collections, never null."
