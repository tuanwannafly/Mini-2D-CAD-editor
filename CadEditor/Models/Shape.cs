using System.Text.Json.Serialization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LineShape), "line")]
[JsonDerivedType(typeof(CircleShape), "circle")]
[JsonDerivedType(typeof(RectangleShape), "rectangle")]
[JsonDerivedType(typeof(PolygonShape), "polygon")]
[JsonDerivedType(typeof(ArcShape), "arc")]
public abstract partial class Shape : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string strokeColor = "#000000";

    [ObservableProperty]
    private double strokeThickness = 2.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderTransform))]
    private double rotationDeg;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderTransform))]
    private double scaleX = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderTransform))]
    private double scaleY = 1.0;

    public abstract BoundingBox GetBounds();

    /// <summary>Axis-aligned bounding box that includes rotation/scale — for handle placement.</summary>
    public BoundingBox GetAxisAlignedBounds()
    {
        var local = GetBounds();
        var c = GetCenter();
        if (RotationDeg == 0 && ScaleX == 1 && ScaleY == 1)
            return local;

        var corners = new[]
        {
            new Point2D(local.MinX, local.MinY),
            new Point2D(local.MaxX, local.MinY),
            new Point2D(local.MaxX, local.MaxY),
            new Point2D(local.MinX, local.MaxY)
        };

        double rad = RotationDeg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var p in corners)
        {
            double dx = (p.X - c.X) * ScaleX;
            double dy = (p.Y - c.Y) * ScaleY;
            double rx = dx * cos - dy * sin + c.X;
            double ry = dx * sin + dy * cos + c.Y;
            if (rx < minX) minX = rx;
            if (rx > maxX) maxX = rx;
            if (ry < minY) minY = ry;
            if (ry > maxY) maxY = ry;
        }
        return new BoundingBox(minX, minY, maxX, maxY);
    }

    public abstract bool HitTest(Point2D point, double tolerance = 3.0);

    protected static double Distance(Point2D a, Point2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    [JsonIgnore]
    public Transform RenderTransform
    {
        get
        {
            var center = GetCenter();
            var matrix = Matrix.Identity;
            matrix.Translate(-center.X, -center.Y);
            matrix.Rotate(RotationDeg);
            matrix.Scale(ScaleX, ScaleY);
            matrix.Translate(center.X, center.Y);
            return new MatrixTransform(matrix);
        }
    }

    public abstract Point2D GetCenter();

    /// <summary>Maps a point from screen space into the shape's local (untransformed) space.</summary>
    public Point2D ToLocal(Point2D p)
    {
        var center = GetCenter();
        double dx = p.X - center.X;
        double dy = p.Y - center.Y;
        double rad = -RotationDeg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double lx = (dx * cos - dy * sin) / ScaleX + center.X;
        double ly = (dx * sin + dy * cos) / ScaleY + center.Y;
        return new Point2D(lx, ly);
    }

    /// <summary>Converts a screen-space delta into the shape's local (untransformed) delta.</summary>
    public Point2D InverseTransformDelta(double dx, double dy)
    {
        if (RotationDeg == 0 && ScaleX == 1 && ScaleY == 1)
            return new Point2D(dx, dy);

        double rad = -RotationDeg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double ldx = dx * cos - dy * sin;
        double ldy = dx * sin + dy * cos;
        if (ScaleX != 0) ldx /= ScaleX;
        if (ScaleY != 0) ldy /= ScaleY;
        return new Point2D(ldx, ldy);
    }
}
