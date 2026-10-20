using System.Globalization;

namespace Lld.Samples.Patterns.Behavioral.Visitor.Modern;

/// <summary>
/// Modern C#: with a closed set of records, an operation is one <c>switch</c> expression; no <c>Accept</c>, no visitor
/// interface. The trade-off: adding a new shape means finding every <c>switch</c> (the compiler can't list them all).
/// </summary>
public abstract record Shape;

public sealed record Circle(double Radius) : Shape;

public sealed record Square(double Side) : Shape;

public static class Exports
{
    public static string ToSvg(Shape shape) => shape switch
    {
        Circle c => string.Create(CultureInfo.InvariantCulture, $"<circle r=\"{c.Radius}\"/>"),
        Square s => string.Create(CultureInfo.InvariantCulture, $"<rect width=\"{s.Side}\" height=\"{s.Side}\"/>"),
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    public static double Area(Shape shape) => shape switch
    {
        Circle c => Math.PI * c.Radius * c.Radius,
        Square s => s.Side * s.Side,
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };
}
