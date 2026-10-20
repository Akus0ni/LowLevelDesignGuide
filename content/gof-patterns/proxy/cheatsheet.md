---
title: "Cheat sheet: Proxy"
---

# Cheat sheet: Proxy

> [!REMEMBER]
> A stand-in that guards the real thing. Your bank card isn't the money; it decides when to reach it.

## Use it when

- The real object is costly: delay or save calls.
- Access needs a guard, or the object is remote.

## The shape

```csharp snippet="samples/Lld.Samples.Patterns/Structural/Proxy/After/CachedRates.cs#CachedRates"
```

## Modern C#

`Lazy<T>` (virtual proxy), `IMemoryCache` / `HybridCache` behind a caching proxy, `DispatchProxy`.

## Don't confuse with

- **Decorator:** adds behaviour; proxy controls access.
- **Adapter:** changes the interface.

## Interview one-liners

- "Virtual, caching, protection, remote."
- "Remote proxies hide network cost: beware loops (N+1)."
