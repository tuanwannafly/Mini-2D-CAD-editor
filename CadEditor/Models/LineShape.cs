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

    public override bool HitTest(Point2D point, double tolerance = 3.0)
    {
        double dx = End.X - Start.X;
        double dy = End.Y - Start.Y;
        double lengthSq = dx * dx + dy * dy;

        if (lengthSq < 1e-12)
            return Distance(point, Start) <= tolerance;

        double t = Math.Clamp(((point.X - Start.X) * dx + (point.Y - Start.Y) * dy) / lengthSq, 0, 1);
        double closestX = Start.X + t * dx;
        double closestY = Start.Y + t * dy;
        return Distance(point, new Point2D(closestX, closestY)) <= tolerance;
    }
}