---
title: "Cheat sheet: Flyweight"
---

# Cheat sheet: Flyweight

> [!REMEMBER]
> Share the heavy part. A forest needs two tree designs, not ten thousand.

## Use it when

- Huge numbers of similar objects; memory measured as a problem.
- Most data repeats and never changes.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Flyweight/After/Forest.cs#TreeTypes.Get"
```

## Modern C#

`FrozenDictionary` of known shared parts; `string.Intern`, `ArrayPool<T>.Shared`.

## Don't confuse with

- **Cache:** saves time; flyweight saves memory.
- **Singleton:** one instance; flyweight is one per distinct value.

## Interview one-liners

- "Intrinsic shared and immutable, extrinsic per object."
- "Measure first."
