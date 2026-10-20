---
title: "Cheat sheet: Command"
---

# Cheat sheet: Command

> [!REMEMBER]
> A request you can hold, queue, undo. The waiter writes your order on a ticket; the kitchen cooks from the ticket.

## Use it when

- You need undo / redo or a history.
- Requests are queued, retried, scheduled or sent elsewhere.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Behavioral/Command/After/Commands.cs#DeleteCommand"
```

## Modern C#

A record of `Do` / `Undo` delegates; serialisable request records + handlers across processes.

## Don't confuse with

- **Strategy:** how to do a job, not what and when.
- **Memento:** a snapshot of state, not an action.

## Interview one-liners

- "Request as an object: queue, log, undo."
- "Invoker doesn't know what the command does."
