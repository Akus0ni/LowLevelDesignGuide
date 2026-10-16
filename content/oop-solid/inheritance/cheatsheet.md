---
title: "Cheat sheet: Inheritance"
---

# Cheat sheet: Inheritance

> [!REMEMBER]
> A child inherits everything, even the bugs. Only inherit from a parent built for it.

## Smells

- Inheriting just to reuse code; overrides that throw `NotSupportedException`.
- Deep trees; `protected` fields; "everything virtual".

## The move

Non-virtual public methods + one small documented hook. Seal the rest.

```csharp snippet="samples/Lld.Samples.OopSolid/Inheritance/After/Playlist.cs#CountingPlaylist"
```

## Good uses

**Template Method** (base owns the algorithm) and framework bases (`IdentityUser`, `DbContext`).

## Traps

- `new` hides; the variable's type picks the method.
- Virtual call in a constructor sees unset fields.

## Interview one-liners

- "Design for inheritance or prohibit it: seal by default."
- "Differ in data? Use a parameter, not a subclass."
