using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class RectangleShapeTests
{
    [Fact]
    public void Constructor_SetsTopLeftWidthHeight()
    {
        var rect = new RectangleShape(new Point2D(2, 3), 10, 5);

        Assert.Equal(new Point2D(2, 3), rect.TopLeft);
        Assert.Equal(10, rect.Width);
        Assert.Equal(5, rect.Height);
    }

    [Fact]
    public void GetBounds_MatchesTopLeftAndSize()
    {
        var rect = new RectangleShape(new Point2D(2, 3), 10, 5);

        var bounds = rect.GetBounds();

        Assert.Equal(2, bounds.MinX);
        Assert.Equal(3, bounds.MinY);
        Assert.Equal(12, bounds.MaxX);
        Assert.Equal(8, bounds.MaxY);
    }
}