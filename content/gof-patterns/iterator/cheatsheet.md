---
title: "Cheat sheet: Iterator"
---

# Cheat sheet: Iterator

> [!REMEMBER]
> Next, next, next: no peeking inside. The Next button plays your playlist; it never shows you the hard drive.

## Use it when

- A class exposes its inner list only so callers can loop.
- You need several ways to walk the same items.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Iterator/Modern/Playlist.cs#Playlist.Unplayed"
```

## Modern C#

`yield return`, LINQ, `IAsyncEnumerable<T>` with `await foreach`.

## Don't confuse with

- **`IQueryable<T>`:** an expression translated by a provider, not an in-memory walk.
- **Visitor:** adds operations to items; iterator only walks them.

## Interview one-liners

- "Access items one by one, storage hidden."
- "Deferred execution: enumerate twice, run twice."
