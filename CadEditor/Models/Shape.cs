using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public abstract partial class Shape : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string strokeColor = "#000000";

    [ObservableProperty]
    private double strokeThickness = 2.0;

    public abstract BoundingBox GetBounds();

    public abstract bool HitTest(Point2D point, double tolerance = 3.0);

    protected static double Distance(Point2D a, Point2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}