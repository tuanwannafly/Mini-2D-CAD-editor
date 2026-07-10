using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultShapes()
    {
        var vm = new MainViewModel();
        var initialCount = vm.Shapes.Count;

        Assert.Equal(5, vm.Shapes.Count);
        vm.Shapes.Add(new LineShape(new Point2D(0, 0), new Point2D(1, 1)));

        Assert.Equal(initialCount + 1, vm.Shapes.Count);
    }

    [Fact]
    public void AddAndRemoveShape_UpdatesCount()
    {
        var vm = new MainViewModel();
        var shape = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var initialCount = vm.Shapes.Count;
        var shape = new CircleShape(new Point2D(0, 0), 5);
        vm.Shapes.Add(shape);
        var before = vm.Shapes.Count;

        Assert.Equal(6, vm.Shapes.Count);

        vm.Shapes.Remove(shape);

        Assert.Equal(5, vm.Shapes.Count);
    }

    [Fact]
    public void SelectTool_SetsCurrentTool()
    {
        var vm = new MainViewModel();

        vm.SelectToolCommand.Execute(DrawingTool.Circle);

        Assert.Equal(DrawingTool.Circle, vm.CurrentTool);
        Assert.Equal(initialCount, vm.Shapes.Count);
    }

    [Fact]
    public void SubmitCliInputCommand_AddsCommandAndResultToLog()
    {
        var vm = new MainViewModel();

        vm.CliInput = "LINE 0,0 10,10";
        vm.SubmitCliInputCommand.Execute(null);

        Assert.Equal(string.Empty, vm.CliInput);
        Assert.Equal("> LINE 0,0 10,10", vm.CliLog[0]);
        Assert.Equal("Đã thực thi lệnh.", vm.CliLog[1]);
    }

    [Fact]
    public void SubmitCliInputCommand_IgnoresBlankInput()
    {
        var vm = new MainViewModel();

        vm.CliInput = "   ";
        vm.SubmitCliInputCommand.Execute(null);

        Assert.Empty(vm.CliLog);
        Assert.Equal("   ", vm.CliInput);
    }

    [Fact]
    public void OnCanvasMouseDown_SelectTool_SelectsShapeByBoundingBox()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        var line = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        vm.Shapes.Add(line);

        vm.OnCanvasMouseDown(new Point2D(5, 5));

        Assert.True(line.IsSelected);
    }

    [Fact]
    public void OnCanvasMouseDown_SelectTool_ClickEmptyAreaDeselectsShape()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        var line = new LineShape(new Point2D(0, 0), new Point2D(10, 10)) { IsSelected = true };
        vm.Shapes.Add(line);

        vm.OnCanvasMouseDown(new Point2D(100, 100));

        Assert.False(line.IsSelected);
    }

    [Fact]
    public void DragSelectedShape_MovesShapeAndUndoRestoresPosition()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        vm.Shapes.Clear();
        var rect = new RectangleShape(new Point2D(10, 20), 30, 40);
        vm.Shapes.Add(rect);

        vm.OnCanvasMouseDown(new Point2D(15, 25));
        vm.OnCanvasMouseMove(new Point2D(35, 45));
        vm.OnCanvasMouseUp(new Point2D(35, 45));
        vm.UndoCommand.Execute(null);

        Assert.Equal(new Point2D(10, 20), rect.TopLeft);
    }
}
