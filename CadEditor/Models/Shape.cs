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
}