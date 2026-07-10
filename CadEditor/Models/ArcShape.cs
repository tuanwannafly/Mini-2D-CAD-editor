using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public partial class ArcShape : Shape
{
    [ObservableProperty]
    private Point2D center;

    [ObservableProperty]
    private double radius;

    [ObservableProperty]
    private double startAngleDeg;

    [ObservableProperty]
    private double endAngleDeg;

    public ArcShape(Point2D center, double radius, double startAngleDeg, double endAngleDeg)
    {
        Center = center;
        Radius = radius;
        StartAngleDeg = startAngleDeg;
        EndAngleDeg = endAngleDeg;
    }

    // Simplified: bbox hình tròn đầy đủ, không cắt theo start/end angle (đủ dùng cho MVP)
    public override BoundingBox GetBounds() => new(
        Center.X - Radius, Center.Y - Radius,
        Center.X + Radius, Center.Y + Radius);
}