using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Models;

public class ShapeTransformStateTests
{
    [Fact]
    public void DefaultTransformValues()
    {
        Shape shape = new RectangleShape(new Point2D(0, 0), 10, 20);

        Assert.Equal(0, shape.RotationDeg);
        Assert.Equal(1.0, shape.ScaleX);
        Assert.Equal(1.0, shape.ScaleY);
    }

    [Fact]
    public void SettingRotation_UpdatesProperty()
    {
        Shape shape = new CircleShape(new Point2D(5, 5), 10);
        shape.RotationDeg = 90;

        Assert.Equal(90, shape.RotationDeg);
    }

    [Fact]
    public void SettingScale_UpdatesProperty()
    {
        Shape shape = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        shape.ScaleX = 2.5;
        shape.ScaleY = 0.5;

        Assert.Equal(2.5, shape.ScaleX);
        Assert.Equal(0.5, shape.ScaleY);
    }

    [Fact]
    public void RenderTransform_IsNotNull()
    {
        Shape shape = new RectangleShape(new Point2D(0, 0), 10, 20);

        Assert.NotNull(shape.RenderTransform);
    }

    [Fact]
    public void IsSelected_DefaultsToFalse()
    {
        Shape shape = new CircleShape(new Point2D(0, 0), 5);

        Assert.False(shape.IsSelected);
    }

    [Fact]
    public void IsSelected_CanBeSet()
    {
        Shape shape = new CircleShape(new Point2D(0, 0), 5);
        shape.IsSelected = true;

        Assert.True(shape.IsSelected);
    }
}
