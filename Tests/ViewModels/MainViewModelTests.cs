using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void AddingShape_IncreasesCollectionCount()
    {
        var vm = new MainViewModel();

        vm.Shapes.Add(new LineShape(new Point2D(0, 0), new Point2D(1, 1)));

        Assert.Single(vm.Shapes);
    }

    [Fact]
    public void RemovingShape_DecreasesCollectionCount()
    {
        var vm = new MainViewModel();
        var shape = new CircleShape(new Point2D(0, 0), 5);
        vm.Shapes.Add(shape);

        vm.Shapes.Remove(shape);

        Assert.Empty(vm.Shapes);
    }
}