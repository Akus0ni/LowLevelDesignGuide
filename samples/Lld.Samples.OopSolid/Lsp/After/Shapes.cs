namespace Lld.Samples.OopSolid.Lsp.After;

/// <summary>The promise every shape keeps: it can tell you its area. Nothing about setting sides.</summary>
public interface IShape
{
    int Area { get; }
}

/// <summary>Immutable: "resizing" gives you a new rectangle, so no caller is surprised.</summary>
public sealed record Rectangle(int Width, int Height) : IShape
{
    public int Area => Width * Height;
}

/// <summary>A sibling, not a child: it keeps the IShape promise and makes no rectangle promises.</summary>
public sealed record Square(int Side) : IShape
{
    public int Area => Side * Side;
}

public static class Shapes
{
    public static int TotalArea(IEnumerable<IShape> shapes) => shapes.Sum(shape => shape.Area);
}
