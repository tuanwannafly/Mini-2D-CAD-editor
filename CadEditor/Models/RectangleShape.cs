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
}