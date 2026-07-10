using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public partial class RectangleShape : Shape
{
    [ObservableProperty]
    private Point2D topLeft;

    [ObservableProperty]
    private double width;

    [ObservableProperty]
    private double height;

    public RectangleShape(Point2D topLeft, double width, double height)
    {
        TopLeft = topLeft;
        Width = width;
        Height = height;
    }

    public override BoundingBox GetBounds() => new(
        TopLeft.X, TopLeft.Y, TopLeft.X + Width, TopLeft.Y + Height);

    public override bool HitTest(Point2D point, double tolerance = 3.0)
    {
        double right = TopLeft.X + Width;
        double bottom = TopLeft.Y + Height;
        double closestX = Math.Clamp(point.X, TopLeft.X, right);
        double closestY = Math.Clamp(point.Y, TopLeft.Y, bottom);
        return Distance(point, new Point2D(closestX, closestY)) <= tolerance;
    }
}