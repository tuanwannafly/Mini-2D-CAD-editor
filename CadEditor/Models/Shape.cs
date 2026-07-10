using System.Windows.Media;
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
