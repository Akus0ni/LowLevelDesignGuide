using System.Numerics;

namespace Lld.Samples.OopSolid.Polymorphism.Language;

/// <summary>A closed family of shapes: the set of types is fixed, the operations on them keep growing.</summary>
public abstract record Shape;

public sealed record Circle(double Radius) : Shape;

public sealed record Rectangle(double Width, double Height) : Shape;

/// <summary>Operations written outside the types, one switch expression each (pattern matching).</summary>
public static class Geometry
{
    public static double Area(Shape shape) => shape switch
    {
        Circle circle => Math.PI * circle.Radius * circle.Radius,
        Rectangle rectangle => rectangle.Width * rectangle.Height,
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown shape."),
    };

    public static double Perimeter(Shape shape) => shape switch
    {
        Circle circle => 2 * Math.PI * circle.Radius,
        Rectangle rectangle => 2 * (rectangle.Width + rectangle.Height),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown shape."),
    };
}

/// <summary>Overloads are picked by the compiler from the variable's type, not by the object at runtime.</summary>
public static class Describer
{
    public static string Describe(Shape shape) => "a shape";

    public static string Describe(Circle circle) => "a circle";
}

/// <summary>Parametric polymorphism: one method for any type that can be compared or added.</summary>
public static class Generic
{
    public static T Largest<T>(IEnumerable<T> items)
        where T : IComparable<T> =>
        items.Aggregate((largest, item) => item.CompareTo(largest) > 0 ? item : largest);

    /// <summary>Generic math (static abstract interface members): works for int, decimal, double, ...</summary>
    public static T Sum<T>(IEnumerable<T> values)
        where T : INumber<T> =>
        values.Aggregate(T.Zero, (total, value) => total + value);
}
