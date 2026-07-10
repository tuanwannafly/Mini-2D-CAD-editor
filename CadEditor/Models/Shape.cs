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
            var matrix = Matrix.Identity;
            matrix.Scale(ScaleX, ScaleY);
            matrix.Rotate(RotationDeg);
            return new MatrixTransform(matrix);
        }
    }
}
