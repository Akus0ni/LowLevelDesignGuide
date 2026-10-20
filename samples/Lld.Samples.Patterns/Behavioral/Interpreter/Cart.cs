namespace Lld.Samples.Patterns.Behavioral.Interpreter;

/// <summary>The context the discount rules are evaluated against. Shared by every version.</summary>
public sealed record Cart(decimal Total, bool IsMember);
