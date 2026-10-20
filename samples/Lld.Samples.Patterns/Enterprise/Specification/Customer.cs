namespace Lld.Samples.Patterns.Enterprise.Specification;

/// <summary>Shared by every version.</summary>
public sealed record Customer(string Name, string Country, bool IsActive, decimal Spent);
