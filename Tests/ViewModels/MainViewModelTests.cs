using System.Linq;
using CadEditor.Models;
using CadEditor.ViewModels;

namespace CadEditor.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultShapes()
    {
        var vm = new MainViewModel();
        int initialCount = vm.Shapes.Count;

        Assert.Equal(5, vm.Shapes.Count);
        vm.Shapes.Add(new LineShape(new Point2D(0, 0), new Point2D(1, 1)));

        Assert.Equal(initialCount + 1, vm.Shapes.Count);
    }

    [Fact]
    public void AddAndRemoveShape_UpdatesCount()
    {
        var vm = new MainViewModel();
        var shape = new CircleShape(new Point2D(0, 0), 5);
        var initialCount = vm.Shapes.Count;
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

    [Fact]
    public void RemoveShapeFromList_UpdatesCount()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        var shape = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
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

    [Fact]
    public void RotateSelectedShape_UpdatesRotationAroundCenter()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        vm.Shapes.Clear();
        var rect = new RectangleShape(new Point2D(10, 20), 30, 40);
        vm.Shapes.Add(rect);
        vm.SelectedShape = rect;

        var center = rect.GetCenter();
        var initialRotation = rect.RotationDeg;

        // Simulate rotating by 90 degrees: start from east, end from north
        var startPoint = new Point2D(center.X + 100, center.Y);
        var endPoint = new Point2D(center.X, center.Y + 100);

        rect.RotationDeg = initialRotation + 90;

        Assert.NotEqual(initialRotation, rect.RotationDeg);
        Assert.Equal(initialRotation + 90, rect.RotationDeg, 5);
    }

    [Fact]
    public void RotateSelectedShape_RotationHandleIsVisible()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        vm.Shapes.Clear();
        var rect = new RectangleShape(new Point2D(10, 20), 30, 40);
        vm.Shapes.Add(rect);
        vm.SelectedShape = rect;

        var rotationHandle = vm.TransformHandles.FirstOrDefault(h => h.Type == HandleType.Rotation);
        Assert.NotNull(rotationHandle);

        // Rotation handle should be perpendicular outward from top edge midpoint
        var bounds = rect.GetAxisAlignedBounds();
        var topMidX = (bounds.MinX + bounds.MaxX) / 2;
        Assert.NotEqual(0, vm.RotationLineX2);
        Assert.NotEqual(0, vm.RotationLineY2);
    }

    [Fact]
    public void DragSelectedShape_TransformHandlesFollowShapeDuringDrag()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        vm.Shapes.Clear();
        var rect = new RectangleShape(new Point2D(10, 20), 30, 40);
        vm.Shapes.Add(rect);

        vm.OnCanvasMouseDown(new Point2D(15, 25));
        var handlesBefore = vm.TransformHandles
            .Select(h => (h.X, h.Y))
            .ToList();
        Assert.Equal(9, handlesBefore.Count);

        vm.OnCanvasMouseMove(new Point2D(115, 125));

        var handlesAfter = vm.TransformHandles
            .Select(h => (h.X, h.Y))
            .ToList();
        Assert.Equal(handlesBefore.Count, handlesAfter.Count);
        for (int i = 0; i < handlesBefore.Count; i++)
        {
            Assert.NotEqual(handlesBefore[i], handlesAfter[i]);
            Assert.Equal(handlesBefore[i].X + 100, handlesAfter[i].X, 5);
            Assert.Equal(handlesBefore[i].Y + 100, handlesAfter[i].Y, 5);
        }

        vm.OnCanvasMouseUp(new Point2D(115, 125));
    }

    [Fact]
    public void DragRotatedShape_FollowsPointerInScreenSpace()
    {
        var vm = new MainViewModel { CurrentTool = DrawingTool.None };
        vm.Shapes.Clear();
        var rect = new RectangleShape(new Point2D(100, 100), 40, 60);
        rect.RotationDeg = 90;
        vm.Shapes.Add(rect);
        vm.SelectedShape = rect;

        var centerBefore = rect.GetCenter();
        vm.OnCanvasMouseDown(new Point2D(centerBefore.X, centerBefore.Y - 50));

        // Drag 100px to the right in screen space. Since MoveBy uses world delta,
        // the shape's geometry center should translate by exactly (100, 0).
        vm.OnCanvasMouseMove(new Point2D(centerBefore.X + 100, centerBefore.Y - 50));

        var centerAfter = rect.GetCenter();

        // World center moves by the world delta regardless of rotation.
        Assert.Equal(100, centerAfter.X - centerBefore.X, 5);
        Assert.Equal(0, centerAfter.Y - centerBefore.Y, 5);

        vm.OnCanvasMouseUp(new Point2D(centerBefore.X + 100, centerBefore.Y - 50));
    }
}
