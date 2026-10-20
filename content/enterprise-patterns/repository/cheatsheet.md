---
title: "Cheat sheet: Repository"
---

# Cheat sheet: Repository

> [!REMEMBER]
> Ask the librarian, not the shelves. You ask for "pending orders"; how they're found is not your problem.

## Use it when

- Queries are copied across handlers.
- Business rules should be tested without a database.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Enterprise/Repository/After/Orders.cs#IOrderRepository"
```

## Modern C#

`DbSet<T>` is one already; named `IQueryable<T>` extensions, or thin repositories per aggregate.

## Don't confuse with

- **DAO:** mirrors tables; repository speaks the domain.
- **Unit of Work:** saves the changes; repository finds and adds.

## Interview one-liners

- "Per aggregate, not per table."
- "Don't wrap `DbSet<T>` in a generic repository for nothing."
