using System.Globalization;

namespace Lld.Samples.Patterns.Behavioral.Visitor.Before;

/// <summary>
/// Every new operation (export to SVG, to JSON, compute area...) is one more method in every shape class. The shapes
/// fill up with export code that has nothing to do with being a shape.
/// </summary>
public interface IShape
{
    double Area();

    string ToSvg();

    string ToJson();
}

public sealed class Circle(double radius) : IShape
{
    public double Area() => Math.PI * radius * radius;

    public string ToSvg() => string.Create(CultureInfo.InvariantCulture, $"<circle r=\"{radius}\"/>");

    public string ToJson() => string.Create(CultureInfo.InvariantCulture, $$"""{"circle":{{radius}}}""");
}

public sealed class Square(double side) : IShape
{
    public double Area() => side * side;

    public string ToSvg() => string.Create(CultureInfo.InvariantCulture, $"<rect width=\"{side}\" height=\"{side}\"/>");

    public string ToJson() => string.Create(CultureInfo.InvariantCulture, $$"""{"square":{{side}}}""");
}
