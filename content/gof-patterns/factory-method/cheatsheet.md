---
title: "Cheat sheet: Factory Method"
---

# Cheat sheet: Factory Method

> [!REMEMBER]
> Same recipe, each shop picks the dough. The base class says *when* to create; the subclass says *what*.

## Use it when

- A `switch` on type creates objects in code that shouldn't change.
- The steps stay the same; only the class varies.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Creational/FactoryMethod/After/ReportExporter.cs#ReportExporter,CsvExporter"
```

## Modern C#

`new ReportExporter(() => new CsvWriter())`: a `Func<T>` or DI instead of a subclass.

## Don't confuse with

- **Simple factory:** one method with a `switch`; adding a type edits it.
- **Abstract Factory:** a whole family, one method per product.

## Interview one-liners

- "A virtual constructor: subclasses choose the class."
- "OCP for object creation."
