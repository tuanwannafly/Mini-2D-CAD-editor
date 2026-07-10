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

    public override Point2D GetCenter() => Center;

    public override bool HitTest(Point2D point, double tolerance = 3.0)
    {
        double dist = Distance(point, Center);
        if (Math.Abs(dist - Radius) > tolerance) return false;

        double angle = Math.Atan2(point.Y - Center.Y, point.X - Center.X) * 180 / Math.PI;
        if (angle < 0) angle += 360;

        double start = StartAngleDeg % 360;
        if (start < 0) start += 360;
        double end = EndAngleDeg % 360;
        if (end < 0) end += 360;

        if (Math.Abs(start - end) < 1e-9)
            return true;

        if (start < end)
            return angle >= start && angle <= end;

        return angle >= start || angle <= end;
    }
}