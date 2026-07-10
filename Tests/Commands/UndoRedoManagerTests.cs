using System.Collections.ObjectModel;
using CadEditor.Commands;
using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Commands;

public class UndoRedoManagerTests
{
    [Fact]
    public void ExecuteCommand_AddsShape_AndEnablesUndo()
    {
        var shapes = new ObservableCollection<Shape>();
        var shape = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        var manager = new UndoRedoManager();

        manager.ExecuteCommand(new AddShapeCommand(shapes, shape));

        Assert.Contains(shape, shapes);
        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Undo_RemovesShape_AndEnablesRedo()
    {
        var shapes = new ObservableCollection<Shape>();
        var shape = new CircleShape(new Point2D(5, 5), 10);
        var manager = new UndoRedoManager();
        manager.ExecuteCommand(new AddShapeCommand(shapes, shape));

        manager.Undo();

        Assert.DoesNotContain(shape, shapes);
        Assert.False(manager.CanUndo);
        Assert.True(manager.CanRedo);
    }

    [Fact]
    public void Redo_ReappliesUndoneCommand()
    {
        var shapes = new ObservableCollection<Shape>();
        var shape = new RectangleShape(new Point2D(0, 0), 20, 20);
        var manager = new UndoRedoManager();
        manager.ExecuteCommand(new AddShapeCommand(shapes, shape));
        manager.Undo();

        manager.Redo();

        Assert.Contains(shape, shapes);
        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void ExecuteCommand_AfterUndo_ClearsRedoStack()
    {
        var shapes = new ObservableCollection<Shape>();
        var shapeA = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var shapeB = new LineShape(new Point2D(2, 2), new Point2D(3, 3));
        var manager = new UndoRedoManager();

        manager.ExecuteCommand(new AddShapeCommand(shapes, shapeA));
        manager.Undo();
        manager.ExecuteCommand(new AddShapeCommand(shapes, shapeB));

        Assert.False(manager.CanRedo);
        Assert.Contains(shapeB, shapes);
        Assert.DoesNotContain(shapeA, shapes);
    }

    [Fact]
    public void Undo_WhenStackEmpty_DoesNotThrow()
    {
        var manager = new UndoRedoManager();
        manager.Undo();
        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void Redo_WhenStackEmpty_DoesNotThrow()
    {
        var manager = new UndoRedoManager();
        manager.Redo();
        Assert.False(manager.CanRedo);
    }
}