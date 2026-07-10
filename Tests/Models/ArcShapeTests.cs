using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class ArcShapeTests
{
    [Fact]
    public void Constructor_SetsCenterRadiusAndAngles()
    {
        var arc = new ArcShape(new Point2D(0, 0), 5, 0, 90);

        Assert.Equal(new Point2D(0, 0), arc.Center);
        Assert.Equal(5, arc.Radius);
        Assert.Equal(0, arc.StartAngleDeg);
        Assert.Equal(90, arc.EndAngleDeg);
    }

    [Fact]
    public void GetBounds_ReturnsFullCircleBoundingBox()
    {
        var arc = new ArcShape(new Point2D(2, 2), 3, 0, 90);

        var bounds = arc.GetBounds();

        Assert.Equal(-1, bounds.MinX);
        Assert.Equal(-1, bounds.MinY);
        Assert.Equal(5, bounds.MaxX);
        Assert.Equal(5, bounds.MaxY);
    }
}