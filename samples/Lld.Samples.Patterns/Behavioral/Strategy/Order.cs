namespace Lld.Samples.Patterns.Behavioral.Strategy;

/// <summary>What shipping is priced on. Shared by every version.</summary>
public sealed record Order(decimal Subtotal, decimal WeightKg);
