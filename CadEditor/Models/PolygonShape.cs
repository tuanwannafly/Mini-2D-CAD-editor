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

    public override bool HitTest(Point2D point, double tolerance = 3.0)
    {
        int count = Vertices.Count;
        if (count == 0) return false;

        // Check distance to each edge segment first
        for (int i = 0; i < count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % count];
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-12)
            {
                if (Distance(point, a) <= tolerance) return true;
                continue;
            }
            double t = Math.Clamp(((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lenSq, 0, 1);
            double cx = a.X + t * dx;
            double cy = a.Y + t * dy;
            if (Distance(point, new Point2D(cx, cy)) <= tolerance) return true;
        }

        // If count < 3, no interior
        if (count < 3) return false;

        // Ray casting for interior
        bool inside = false;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            var vi = Vertices[i];
            var vj = Vertices[j];
            if ((vi.Y > point.Y) != (vj.Y > point.Y) &&
                point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
            {
                inside = !inside;
            }
        }
        return inside;
    }
}