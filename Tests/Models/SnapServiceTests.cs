using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class SnapServiceTests
{
    [Theory]
    [InlineData(3.0, 10.0, 0.0, 0.0)]
    [InlineData(7.0, 10.0, 10.0, 10.0)]
    [InlineData(12.0, 10.0, 10.0, 10.0)]
    [InlineData(14.0, 10.0, 10.0, 10.0)]
    [InlineData(16.0, 10.0, 20.0, 20.0)]
    [InlineData(5.0, 5.0, 5.0, 5.0)]
    [InlineData(8.0, 5.0, 10.0, 10.0)]
    [InlineData(15.0, 10.0, 20.0, 20.0)]
    public void SnapToGrid_RoundsToNearestGridLine(double value, double gridSize, double expectedX, double expectedY)
    {
        var point = new Point2D(value, value);
        var snapped = SnapService.SnapToGrid(point, gridSize);
        Assert.Equal(expectedX, snapped.X);
        Assert.Equal(expectedY, snapped.Y);
    }

    [Fact]
    public void SnapToGrid_WithZeroOrNegativeGridSize_ReturnsOriginalPoint()
    {
        var point = new Point2D(17.3, 42.8);
        Assert.Equal(point, SnapService.SnapToGrid(point, 0));
        Assert.Equal(point, SnapService.SnapToGrid(point, -5));
    }

    [Fact]
    public void SnapToGrid_RoundsEachAxisIndependently()
    {
        var point = new Point2D(23.0, 44.0);
        var snapped = SnapService.SnapToGrid(point, 10.0);
        Assert.Equal(20.0, snapped.X);
        Assert.Equal(40.0, snapped.Y);
    }

    [Fact]
    public void SnapToGrid_WithNonDefaultGridSize()
    {
        var point = new Point2D(24.0, 36.0);
        var snapped = SnapService.SnapToGrid(point, 25.0);
        Assert.Equal(25.0, snapped.X);
        Assert.Equal(25.0, snapped.Y);
    }

    [Fact]
    public void SnapToPoint_ReturnsClosestPointWithinThreshold()
    {
        var point = new Point2D(15.0, 15.0);
        var snapPoints = new[] { new Point2D(0, 0), new Point2D(10, 10), new Point2D(20, 20) };
        var snapped = SnapService.SnapToPoint(point, snapPoints, 10.0);
        Assert.Equal(new Point2D(10, 10), snapped);
    }

    [Fact]
    public void SnapToPoint_ReturnsPointAtThresholdDistance()
    {
        var point = new Point2D(10.0, 0.0);
        var snapPoints = new[] { new Point2D(0, 0) };
        var snapped = SnapService.SnapToPoint(point, snapPoints, 10.0);
        Assert.Equal(new Point2D(0, 0), snapped);
    }

    [Fact]
    public void SnapToPoint_ReturnsOriginalPoint_WhenNoPointsWithinThreshold()
    {
        var point = new Point2D(100.0, 100.0);
        var snapPoints = new[] { new Point2D(0, 0) };
        var snapped = SnapService.SnapToPoint(point, snapPoints, 10.0);
        Assert.Equal(point, snapped);
    }

    [Fact]
    public void SnapToPoint_ReturnsClosestPoint_WithMultipleCandidates()
    {
        var point = new Point2D(5.0, 0.0);
        var snapPoints = new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(0, 10) };
        var snapped = SnapService.SnapToPoint(point, snapPoints, 10.0);
        Assert.Equal(new Point2D(0, 0), snapped);
    }

    [Fact]
    public void SnapToPoint_WithEmptySnapPoints_ReturnsOriginal()
    {
        var point = new Point2D(5.0, 5.0);
        var snapped = SnapService.SnapToPoint(point, Array.Empty<Point2D>(), 10.0);
        Assert.Equal(point, snapped);
    }

    [Fact]
    public void Snap_AppliesGridSnapOnly_WhenPointSnapDisabled()
    {
        var point = new Point2D(14.0, 14.0);
        var snapped = SnapService.Snap(point, 10.0, true, null, 10.0, false);
        Assert.Equal(new Point2D(10.0, 10.0), snapped);
    }

    [Fact]
    public void Snap_AppliesPointSnapOnly_WhenGridSnapDisabled()
    {
        var point = new Point2D(5.0, 0.0);
        var snapPoints = new[] { new Point2D(0, 0) };
        var snapped = SnapService.Snap(point, 10.0, false, snapPoints, 10.0, true);
        Assert.Equal(new Point2D(0, 0), snapped);
    }

    [Fact]
    public void Snap_AppliesBothGridAndPointSnap()
    {
        var point = new Point2D(14.0, 14.0);
        var snapPoints = new[] { new Point2D(10, 10) };
        var snapped = SnapService.Snap(point, 10.0, true, snapPoints, 10.0, true);
        Assert.Equal(new Point2D(10, 10), snapped);
    }

    [Fact]
    public void Snap_ReturnsOriginal_WhenBothDisabled()
    {
        var point = new Point2D(14.0, 14.0);
        var snapped = SnapService.Snap(point, 10.0, false, null, 10.0, false);
        Assert.Equal(point, snapped);
    }

    [Fact]
    public void GetSnapPoints_ReturnsLineEndpoints()
    {
        var shapes = new Shape[] { new LineShape(new Point2D(0, 0), new Point2D(10, 10)) };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        Assert.Contains(new Point2D(0, 0), points);
        Assert.Contains(new Point2D(10, 10), points);
    }

    [Fact]
    public void GetSnapPoints_ReturnsCircleCenter()
    {
        var shapes = new Shape[] { new CircleShape(new Point2D(50, 50), 20) };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        Assert.Contains(new Point2D(50, 50), points);
    }

    [Fact]
    public void GetSnapPoints_ReturnsRectangleCorners()
    {
        var shapes = new Shape[] { new RectangleShape(new Point2D(10, 10), 30, 20) };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        Assert.Contains(new Point2D(10, 10), points);
        Assert.Contains(new Point2D(40, 10), points);
        Assert.Contains(new Point2D(40, 30), points);
        Assert.Contains(new Point2D(10, 30), points);
    }

    [Fact]
    public void GetSnapPoints_ReturnsPolygonVertices()
    {
        var vertices = new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(5, 10) };
        var shapes = new Shape[] { new PolygonShape(vertices) };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        foreach (var v in vertices)
            Assert.Contains(v, points);
    }

    [Fact]
    public void GetSnapPoints_ReturnsArcCenter()
    {
        var shapes = new Shape[] { new ArcShape(new Point2D(100, 100), 30, 0, 90) };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        Assert.Contains(new Point2D(100, 100), points);
    }

    [Fact]
    public void GetSnapPoints_ReturnsPointsFromMultipleShapes()
    {
        var shapes = new Shape[]
        {
            new LineShape(new Point2D(1, 1), new Point2D(2, 2)),
            new CircleShape(new Point2D(3, 3), 5),
        };
        var points = SnapService.GetSnapPoints(shapes).ToList();
        Assert.Contains(new Point2D(1, 1), points);
        Assert.Contains(new Point2D(2, 2), points);
        Assert.Contains(new Point2D(3, 3), points);
    }
}
