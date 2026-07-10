using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public partial class LineShape : Shape
{
    [ObservableProperty]
    private Point2D start;

    [ObservableProperty]
    private Point2D end;

    public LineShape(Point2D start, Point2D end)
    {
        Start = start;
        End = end;
    }

    public override BoundingBox GetBounds() => new(
        Math.Min(Start.X, End.X), Math.Min(Start.Y, End.Y),
        Math.Max(Start.X, End.X), Math.Max(Start.Y, End.Y));
}