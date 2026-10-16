using System.Numerics;

namespace Lld.Samples.OopSolid.InterfacesVsAbstractClasses.Language;

/// <summary>
/// A default interface method lets a published interface grow without breaking the classes that already implement it.
/// It can't hold state: that is still an abstract class's job.
/// </summary>
public interface IGreeter
{
    string Greet(string name);

    /// <summary>Added in version 2: old implementations get this body for free.</summary>
    string GreetAll(IEnumerable<string> names) => string.Join(" ", names.Select(Greet));
}

public sealed class PoliteGreeter : IGreeter
{
    public string Greet(string name) => $"Hello, {name}.";
}

/// <summary>A static abstract member: every implementing type must provide a static <c>Zero</c>.</summary>
public interface IHasZero<TSelf>
    where TSelf : IHasZero<TSelf>
{
    static abstract TSelf Zero { get; }
}

public readonly record struct Points(int Value) : IHasZero<Points>, IAdditionOperators<Points, Points, Points>
{
    public static Points Zero => new(0);

    public static Points operator +(Points left, Points right) => new(left.Value + right.Value);
}

public static class Totals
{
    public static T Sum<T>(IEnumerable<T> items)
        where T : IHasZero<T>, IAdditionOperators<T, T, T> =>
        items.Aggregate(T.Zero, (total, item) => total + item);
}
