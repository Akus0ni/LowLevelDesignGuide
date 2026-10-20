using System.Globalization;

namespace Lld.Samples.Patterns.Behavioral.Visitor.After;

/// <summary>The visitor: one method per kind of shape. Each operation is one visitor class.</summary>
public interface IShapeVisitor<out T>
{
    T VisitCircle(Circle circle);

    T VisitSquare(Square square);
}

/// <summary>The element: shapes only know how to accept a visitor (double dispatch), nothing about exports.</summary>
public interface IShape
{
    T Accept<T>(IShapeVisitor<T> visitor);
}

public sealed class Circle(double radius) : IShape
{
    public double Radius { get; } = radius;

    public T Accept<T>(IShapeVisitor<T> visitor) => visitor.VisitCircle(this);
}

public sealed class Square(double side) : IShape
{
    public double Side { get; } = side;

    public T Accept<T>(IShapeVisitor<T> visitor) => visitor.VisitSquare(this);
}

/// <summary>A new operation is a new class; the shapes are not opened.</summary>
public sealed class SvgExport : IShapeVisitor<string>
{
    public string VisitCircle(Circle circle) => string.Create(CultureInfo.InvariantCulture, $"<circle r=\"{circle.Radius}\"/>");

    public string VisitSquare(Square square) =>
        string.Create(CultureInfo.InvariantCulture, $"<rect width=\"{square.Side}\" height=\"{square.Side}\"/>");
}

public sealed class AreaCalculator : IShapeVisitor<double>
{
    public double VisitCircle(Circle circle) => Math.PI * circle.Radius * circle.Radius;

    public double VisitSquare(Square square) => square.Side * square.Side;
}
