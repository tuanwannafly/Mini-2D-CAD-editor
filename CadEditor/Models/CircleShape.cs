using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public partial class CircleShape : Shape
{
    [ObservableProperty]
    private Point2D center;

    [ObservableProperty]
    private double radius;

    public CircleShape(Point2D center, double radius)
    {
        Center = center;
        Radius = radius;
    }

    public override BoundingBox GetBounds() => new(
        Center.X - Radius, Center.Y - Radius,
        Center.X + Radius, Center.Y + Radius);

    public override Point2D GetCenter() => Center;

    public override bool HitTest(Point2D point, double tolerance = 3.0)
    {
        return Math.Abs(Distance(point, Center) - Radius) <= tolerance;
    }
}