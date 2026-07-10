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
}