using CommunityToolkit.Mvvm.ComponentModel;

namespace CadEditor.Models;

public partial class PolygonShape : Shape
{
    [ObservableProperty]
    private List<Point2D> vertices = new();

    public PolygonShape() { }

    public PolygonShape(IEnumerable<Point2D> vertices)
    {
        Vertices = vertices.ToList();
    }

    public void AddVertex(Point2D point)
    {
        Vertices.Add(point);
        OnPropertyChanged(nameof(Vertices));
    }

    public override BoundingBox GetBounds()
    {
        if (Vertices.Count == 0) return new BoundingBox(0, 0, 0, 0);

        return new BoundingBox(
            Vertices.Min(v => v.X), Vertices.Min(v => v.Y),
            Vertices.Max(v => v.X), Vertices.Max(v => v.Y));
    }
    
    public void UpdateLastVertex(Point2D point)
    {
        if (Vertices.Count == 0) return;
        Vertices[^1] = point;
        OnPropertyChanged(nameof(Vertices));
    }

    public void RemoveLastVertex()
    {
        if (Vertices.Count == 0) return;
        Vertices.RemoveAt(Vertices.Count - 1);
        OnPropertyChanged(nameof(Vertices));
    }
}