---
title: "Cheat sheet: Specification"
---

# Cheat sheet: Specification

> [!REMEMBER]
> Name the rule, then combine rules. "Active", "in India", "spent over ₹10,000": three rules, any mix.

## Use it when

- Repositories grow a method per filter combination.
- One rule is written in many places.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Enterprise/Specification/After/Specifications.cs#Specification"
```

## Modern C#

Specs on `Expression<Func<T, bool>>`: in memory and SQL from one rule; Ardalis.Specification.

## Don't confuse with

- **Strategy:** how to do a job; a spec says yes or no.
- **Interpreter:** a whole language; specs are its business-rule cousin.

## Interview one-liners

- "One rule, one class, combine with And / Or."
- "Use expressions so EF Core can run it as SQL."
