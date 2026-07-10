using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class PolygonShapeTests
{
    [Fact]
    public void Constructor_SetsInitialVertices()
    {
        var points = new[] { new Point2D(0, 0), new Point2D(4, 0), new Point2D(4, 4) };

        var polygon = new PolygonShape(points);

        Assert.Equal(3, polygon.Vertices.Count);
        Assert.Equal(points[2], polygon.Vertices[2]);
    }

    [Fact]
    public void AddVertex_AppendsToList()
    {
        var polygon = new PolygonShape(new[] { new Point2D(0, 0) });

        polygon.AddVertex(new Point2D(5, 5));

        Assert.Equal(2, polygon.Vertices.Count);
        Assert.Equal(new Point2D(5, 5), polygon.Vertices[1]);
    }

    [Fact]
    public void GetBounds_ReturnsMinMaxAcrossVertices()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(1, 5), new Point2D(4, 1), new Point2D(2, 8)
        });

        var bounds = polygon.GetBounds();

        Assert.Equal(1, bounds.MinX);
        Assert.Equal(1, bounds.MinY);
        Assert.Equal(4, bounds.MaxX);
        Assert.Equal(8, bounds.MaxY);
    }

    [Fact]
    public void GetBounds_ReturnsZero_WhenNoVertices()
    {
        var polygon = new PolygonShape();

        var bounds = polygon.GetBounds();

        Assert.Equal(0, bounds.Width);
        Assert.Equal(0, bounds.Height);
    }
}