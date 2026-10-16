namespace Lld.Samples.OopSolid.Lsp.Before;

/// <summary>A rectangle whose sides can be set one at a time.</summary>
public class Rectangle
{
    public virtual int Width { get; set; }

    public virtual int Height { get; set; }

    public int Area => Width * Height;
}

/// <summary>"A square is a rectangle", so... setting one side has to change the other.</summary>
public sealed class Square : Rectangle
{
    public override int Width
    {
        get => base.Width;
        set => base.Width = base.Height = value;
    }

    public override int Height
    {
        get => base.Height;
        set => base.Width = base.Height = value;
    }
}

/// <summary>Written for rectangles, and correct for every real rectangle.</summary>
public static class Resizer
{
    public static int StretchToTenByFive(Rectangle rectangle)
    {
        ArgumentNullException.ThrowIfNull(rectangle);
        rectangle.Width = 10;
        rectangle.Height = 5;
        return rectangle.Area;
    }
}
