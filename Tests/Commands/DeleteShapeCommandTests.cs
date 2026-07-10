using System.Collections.ObjectModel;
using CadEditor.Commands;
using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Commands;

public class DeleteShapeCommandTests
{
    [Fact]
    public void Execute_RemovesShapeFromCollection()
    {
        var shape = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var shapes = new ObservableCollection<Shape> { shape };
        var command = new DeleteShapeCommand(shapes, shape);

        command.Execute();

        Assert.DoesNotContain(shape, shapes);
    }

    [Fact]
    public void Undo_RestoresShapeAtOriginalIndex()
    {
        var shapeA = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var shapeB = new CircleShape(new Point2D(5, 5), 10);
        var shapeC = new RectangleShape(new Point2D(2, 2), 4, 4);
        var shapes = new ObservableCollection<Shape> { shapeA, shapeB, shapeC };
        var command = new DeleteShapeCommand(shapes, shapeB);

        command.Execute();
        command.Undo();

        Assert.Equal(new Shape[] { shapeA, shapeB, shapeC }, shapes);
    }
}
