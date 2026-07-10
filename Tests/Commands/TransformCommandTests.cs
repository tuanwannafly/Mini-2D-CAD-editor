using CadEditor.Commands;
using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Commands;

public class TransformCommandTests
{
    [Fact]
    public void Execute_SetsTransformValues()
    {
        var shape = new RectangleShape(new Point2D(0, 0), 100, 50);
        var cmd = new TransformCommand(shape, 0, 1, 1, 45, 2, 1.5);

        cmd.Execute();

        Assert.Equal(45, shape.RotationDeg);
        Assert.Equal(2.0, shape.ScaleX);
        Assert.Equal(1.5, shape.ScaleY);
    }

    [Fact]
    public void Undo_RestoresOriginalValues()
    {
        var shape = new RectangleShape(new Point2D(0, 0), 100, 50);
        shape.RotationDeg = 45;
        shape.ScaleX = 2;
        shape.ScaleY = 1.5;
        var cmd = new TransformCommand(shape, 0, 1, 1, 45, 2, 1.5);

        cmd.Execute();
        cmd.Undo();

        Assert.Equal(0, shape.RotationDeg);
        Assert.Equal(1.0, shape.ScaleX);
        Assert.Equal(1.0, shape.ScaleY);
    }

    [Fact]
    public void ExecuteAfterUndo_ReappliesTransform()
    {
        var shape = new RectangleShape(new Point2D(0, 0), 100, 50);
        var cmd = new TransformCommand(shape, 0, 1, 1, -90, 0.5, 2);

        cmd.Execute();
        cmd.Undo();
        cmd.Execute();

        Assert.Equal(-90, shape.RotationDeg);
        Assert.Equal(0.5, shape.ScaleX);
        Assert.Equal(2.0, shape.ScaleY);
    }
}
