using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class LineShapeTests
{
    [Fact]
    public void Constructor_SetsStartAndEnd()
    {
        var start = new Point2D(0, 0);
        var end = new Point2D(10, 10);

        var line = new LineShape(start, end);

        Assert.Equal(start, line.Start);
        Assert.Equal(end, line.End);
    }

    [Fact]
    public void Constructor_DefaultsToNotSelected_BlackStroke()
    {
        var line = new LineShape(new Point2D(0, 0), new Point2D(1, 1));

        Assert.False(line.IsSelected);
        Assert.Equal("#000000", line.StrokeColor);
    }

    [Fact]
    public void GetBounds_ReturnsCorrectBoundingBox_ForDiagonalLine()
    {
        var line = new LineShape(new Point2D(5, 8), new Point2D(1, 2));

        var bounds = line.GetBounds();

        Assert.Equal(1, bounds.MinX);
        Assert.Equal(2, bounds.MinY);
        Assert.Equal(5, bounds.MaxX);
        Assert.Equal(8, bounds.MaxY);
    }

    [Fact]
    public void Id_IsUniquePerInstance()
    {
        var a = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var b = new LineShape(new Point2D(0, 0), new Point2D(1, 1));

        Assert.NotEqual(a.Id, b.Id);
    }
}