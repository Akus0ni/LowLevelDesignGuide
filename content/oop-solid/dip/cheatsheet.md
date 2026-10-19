---
title: "Cheat sheet: Dependency Inversion (DIP)"
---

# Cheat sheet: Dependency Inversion (DIP)

> [!REMEMBER]
> Depend on the socket, not the wiring behind the wall.

## Smells

- `new SqlConnection` / `new SmtpClient` in business code.
- Can't test without a database or network.

## The move

Business code owns an interface in its own words; details implement it.

```csharp snippet="samples/Lld.Samples.OopSolid/Dip/After/Checkout.cs#ICustomerNotifier"
```

## DIP vs DI

**DIP:** who owns the interface. **DI:** passing dependencies in. **Container:** a tool for DI.

## In this app

Application owns `IAccessRequestRepository`; EF Core implements it in Infrastructure.

## Interview one-liners

- "Dependencies point inwards, to interfaces the core owns."
- "You can use DI and still break DIP."
