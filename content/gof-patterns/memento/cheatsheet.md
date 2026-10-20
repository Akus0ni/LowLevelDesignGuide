---
title: "Cheat sheet: Memento"
---

# Cheat sheet: Memento

> [!REMEMBER]
> Save the game, not the code. A save point brings everything back; you never see what's inside the file.

## Use it when

- You need undo or checkpoints for an object with private state.
- Outside code reads and writes fields just to restore them.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Memento/After/Editor.cs#Editor.Save,Editor.Restore"
```

## Modern C#

Immutable `record` state: the previous value already is the snapshot.

## Don't confuse with

- **Command:** undoes by reversing an action.
- **Prototype:** copies to make new objects, not to restore.

## Interview one-liners

- "Originator, memento, caretaker."
- "Only the originator can read the snapshot."
