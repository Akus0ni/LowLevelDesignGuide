---
title: "Cheat sheet: Composite"
---

# Cheat sheet: Composite

> [!REMEMBER]
> One or many, treat them the same. Ask a folder its size: it asks everything inside.

## Use it when

- Your data is a tree (folders, menus, bundles, UI).
- Callers check types and recurse by hand.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Composite/After/FileSystem.cs#Folder"
```

## Modern C#

Fixed node kinds: records + `switch`. New kinds keep coming: composite.

## Don't confuse with

- **Decorator:** wraps one object; composite holds many.
- **Visitor:** adds operations to a composite tree.

## Interview one-liners

- "Leaf and group share one interface."
- "`Add` on the composite only, or leaves break LSP."
