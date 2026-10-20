---
title: "Cheat sheet: Relationships: the six arrows"
---

# Cheat sheet: Relationships: the six arrows

> [!REMEMBER]
> **Solid** line: it holds one. **Dashed**: it uses or implements one. **Triangle**: is a. **Diamond**: owns (filled =
> the parts die with it).

## Draw it when

Ownership, lifetimes or an interface between classes matter.

## The symbols

```uml-arrows
```

## From C#

A field → `-->`; a list it fills itself → `*--`; a list handed in → `o--`; a base class → `<|--`; an interface
→ `<|..`; a parameter or `new` → `..>`.

## Mistakes

Inheritance arrow pointing at the child; composition for shared parts; a two-way line by default.

## Interview one-liners

- "Composition: the parts die with the whole. Aggregation: they don't."
- "Dashed means uses or implements; solid means holds."
- "Arrows point from the one that knows to the one it knows."
