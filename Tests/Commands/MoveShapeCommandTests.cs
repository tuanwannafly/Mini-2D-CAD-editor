using CadEditor.Commands;
using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Commands;

public class MoveShapeCommandTests
{
    [Fact]
    public void Execute_MovesLineToNewPosition()
    {
        var line = new LineShape(new Point2D(1, 2), new Point2D(4, 6));
        var command = new MoveShapeCommand(line, new Point2D(1, 2), new Point2D(11, 12));

        command.Execute();

        Assert.Equal(new Point2D(11, 12), line.Start);
        Assert.Equal(new Point2D(14, 16), line.End);
    }

    [Fact]
    public void Undo_RestoresCircleOriginalPosition()
    {
        var circle = new CircleShape(new Point2D(10, 10), 5);
        var command = new MoveShapeCommand(circle, new Point2D(5, 5), new Point2D(15, 25));

        command.Execute();
        command.Undo();

        Assert.Equal(new Point2D(10, 10), circle.Center);
    }
}
