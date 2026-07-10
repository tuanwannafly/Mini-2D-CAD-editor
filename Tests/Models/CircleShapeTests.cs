using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class CircleShapeTests
{
    [Fact]
    public void Constructor_SetsCenterAndRadius()
    {
        var circle = new CircleShape(new Point2D(5, 5), 3);

        Assert.Equal(new Point2D(5, 5), circle.Center);
        Assert.Equal(3, circle.Radius);
    }

    [Fact]
    public void GetBounds_ReturnsSquareBoundingBox()
    {
        var circle = new CircleShape(new Point2D(10, 10), 4);

        var bounds = circle.GetBounds();

        Assert.Equal(6, bounds.MinX);
        Assert.Equal(6, bounds.MinY);
        Assert.Equal(14, bounds.MaxX);
        Assert.Equal(14, bounds.MaxY);
    }
}