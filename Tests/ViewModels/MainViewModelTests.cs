using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultShapes()
    {
        var vm = new MainViewModel();

        Assert.Equal(5, vm.Shapes.Count);
    }

    [Fact]
    public void AddAndRemoveShape_UpdatesCount()
    {
        var vm = new MainViewModel();
        var shape = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        vm.Shapes.Add(shape);

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
    }
}