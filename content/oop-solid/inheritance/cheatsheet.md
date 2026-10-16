---
title: "Cheat sheet: Inheritance"
---

# Cheat sheet: Inheritance

## In one line

A subclass **is a** kind of its base: it gets the base's type (subtyping) **and** its code (reuse), which couples it
to the base's implementation.

## Smells

- Inheriting to reuse code (`Stack : List<T>`).
- Overrides that throw `NotSupportedException` or do nothing.
- Deep hierarchies; `protected` fields; "everything virtual".
- Overrides that undo what `base.Method()` did.

## The move

Design the base for extension: non-virtual public methods, one small documented hook; seal the rest.

```csharp snippet="samples/Lld.Samples.OopSolid/Inheritance/After/Playlist.cs#CountingPlaylist"
```

## Good uses

- **Template Method:** base owns the algorithm, subclasses fill one step (`ContactImporter.ReadLine`).
- **Framework extension points:** `IdentityUser`, `DbContext`, `ControllerBase`, test fixtures.

## In this app

`AggregateRoot` (one small base, nothing to override), `AppUser : IdentityUser`, everything else `sealed` (CA1852).

## C# traps

- `new` hides; the variable's type picks the method.
- Virtual call in a constructor sees unset subclass fields (CA2214).
- Extension members (C# 14) add helpers without a subclass.

## Interview one-liners

- "Design for inheritance or prohibit it: seal by default."
- "Inherit for is-a and a designed base; compose for reuse and runtime variety."
