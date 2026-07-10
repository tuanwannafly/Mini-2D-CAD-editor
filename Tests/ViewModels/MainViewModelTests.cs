using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void AddingShape_IncreasesCollectionCount()
    {
        var vm = new MainViewModel();
        int initialCount = vm.Shapes.Count;

        vm.Shapes.Add(new LineShape(new Point2D(0, 0), new Point2D(1, 1)));

        Assert.Equal(initialCount + 1, vm.Shapes.Count);
    }

    [Fact]
    public void RemovingShape_DecreasesCollectionCount()
    {
        var vm = new MainViewModel();
        var shape = new CircleShape(new Point2D(0, 0), 5);
        vm.Shapes.Add(shape);
        int countAfterAdd = vm.Shapes.Count;

        vm.Shapes.Remove(shape);

        Assert.Equal(countAfterAdd - 1, vm.Shapes.Count);
    }

    [Fact]
    public void DrawingLine_WithSnapToGridEnabled_SnapsCoordinates()
    {
        var vm = new MainViewModel
        {
            CurrentTool = DrawingTool.Line,
            SnapToGridEnabled = true,
            GridSize = 10,
        };
        int initialCount = vm.Shapes.Count;

        vm.OnCanvasMouseDown(new Point2D(13, 16));
        vm.OnCanvasMouseUp(new Point2D(26, 31));

        Assert.Equal(initialCount + 1, vm.Shapes.Count);
        var line = Assert.IsType<LineShape>(vm.Shapes.Last());
        Assert.Equal(new Point2D(10, 20), line.Start);
        Assert.Equal(new Point2D(30, 30), line.End);
    }

    [Fact]
    public void DrawingLine_WithSnapToPointEnabled_SnapsToExistingEndpoint()
    {
        var vm = new MainViewModel
        {
            CurrentTool = DrawingTool.Line,
            SnapToPointEnabled = true,
        };
        var existingEndpoint = new Point2D(100, 100);
        vm.Shapes.Add(new LineShape(existingEndpoint, new Point2D(150, 150)));
        int initialCount = vm.Shapes.Count;

        vm.OnCanvasMouseDown(new Point2D(104, 103));
        vm.OnCanvasMouseUp(new Point2D(130, 130));

        Assert.Equal(initialCount + 1, vm.Shapes.Count);
        var line = Assert.IsType<LineShape>(vm.Shapes.Last());
        Assert.Equal(existingEndpoint, line.Start);
    }
}
