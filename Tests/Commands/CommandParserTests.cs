using System.Collections.ObjectModel;
using CadEditor.Cli;
using CadEditor.Commands;
using CadEditor.Models;
using Xunit;

namespace CadEditor.Tests.Cli;

public class CommandParserTests
{
    [Fact]
    public void Parse_Line_ReturnsAddShapeCliCommandWithLineShape()
    {
        var shapes = new ObservableCollection<Shape>();

        var result = CommandParser.Parse("LINE 0,0 10,10", shapes);

        Assert.True(result.Success);
        var command = GetEditorCommand<AddShapeCommand>(result);
        command.Execute();
        var line = Assert.IsType<LineShape>(Assert.Single(shapes));
        Assert.Equal(new Point2D(0, 0), line.Start);
        Assert.Equal(new Point2D(10, 10), line.End);
    }

    [Fact]
    public void Parse_Circle_ReturnsAddShapeCliCommandWithCircleShape()
    {
        var shapes = new ObservableCollection<Shape>();

        var result = CommandParser.Parse("CIRCLE 5,5 R3", shapes);

        Assert.True(result.Success);
        GetEditorCommand<AddShapeCommand>(result).Execute();
        var circle = Assert.IsType<CircleShape>(Assert.Single(shapes));
        Assert.Equal(new Point2D(5, 5), circle.Center);
        Assert.Equal(3, circle.Radius);
    }

    [Fact]
    public void Parse_Circle_LowercaseR_IsAccepted()
    {
        var shapes = new ObservableCollection<Shape>();

        var result = CommandParser.Parse("CIRCLE 5,5 r3", shapes);

        Assert.True(result.Success);
        GetEditorCommand<AddShapeCommand>(result).Execute();
        var circle = Assert.IsType<CircleShape>(Assert.Single(shapes));
        Assert.Equal(3, circle.Radius);
    }

    [Fact]
    public void Parse_Rect_ReturnsAddShapeCliCommandWithRectangleShape()
    {
        var shapes = new ObservableCollection<Shape>();

        var result = CommandParser.Parse("RECT 10,10 50,30", shapes);

        Assert.True(result.Success);
        GetEditorCommand<AddShapeCommand>(result).Execute();
        var rect = Assert.IsType<RectangleShape>(Assert.Single(shapes));
        Assert.Equal(new Point2D(10, 10), rect.TopLeft);
        Assert.Equal(50, rect.Width);
        Assert.Equal(30, rect.Height);
    }

    [Fact]
    public void Parse_Arc_ReturnsAddShapeCliCommandWithArcShape()
    {
        var shapes = new ObservableCollection<Shape>();

        var result = CommandParser.Parse("ARC 100,100 R50 0 180", shapes);

        Assert.True(result.Success);
        GetEditorCommand<AddShapeCommand>(result).Execute();
        var arc = Assert.IsType<ArcShape>(Assert.Single(shapes));
        Assert.Equal(new Point2D(100, 100), arc.Center);
        Assert.Equal(50, arc.Radius);
        Assert.Equal(0, arc.StartAngleDeg);
        Assert.Equal(180, arc.EndAngleDeg);
    }

    [Fact]
    public void Parse_Delete_WithValidGuid_ReturnsDeleteShapeCommand()
    {
        var shape = new LineShape(new Point2D(0, 0), new Point2D(1, 1));
        var shapes = new ObservableCollection<Shape> { shape };

        var result = CommandParser.Parse($"DELETE {shape.Id}", shapes);

        Assert.True(result.Success);
        GetEditorCommand<DeleteShapeCommand>(result).Execute();
        Assert.Empty(shapes);
    }

    [Fact]
    public void Parse_Undo_ReturnsUndoCliCommand()
    {
        var result = CommandParser.Parse("UNDO", new ObservableCollection<Shape>());
        Assert.True(result.Success);
        Assert.IsType<UndoCliCommand>(result.Command);
    }

    [Fact]
    public void Parse_Redo_ReturnsRedoCliCommand()
    {
        var result = CommandParser.Parse("REDO", new ObservableCollection<Shape>());
        Assert.True(result.Success);
        Assert.IsType<RedoCliCommand>(result.Command);
    }

    [Fact]
    public void Parse_IsCaseInsensitiveForKeyword()
    {
        var result = CommandParser.Parse("line 0,0 5,5", new ObservableCollection<Shape>());
        Assert.True(result.Success);
        GetEditorCommand<AddShapeCommand>(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ReturnsFailure(string input)
    {
        var result = CommandParser.Parse(input, new ObservableCollection<Shape>());
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_UnknownKeyword_ReturnsFailure()
    {
        var result = CommandParser.Parse("TRIANGLE 0,0 1,1", new ObservableCollection<Shape>());
        Assert.False(result.Success);
        Assert.Contains("Lệnh không hợp lệ", result.ErrorMessage);
    }

    [Fact]
    public void Parse_Line_WrongArgCount_ReturnsFailure()
    {
        var result = CommandParser.Parse("LINE 0,0", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_Line_BadPointFormat_ReturnsFailure()
    {
        var result = CommandParser.Parse("LINE 0-0 10,10", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_Circle_MissingRPrefix_ReturnsFailure()
    {
        var result = CommandParser.Parse("CIRCLE 5,5 3", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_Circle_NonNumericRadius_ReturnsFailure()
    {
        var result = CommandParser.Parse("CIRCLE 5,5 Rxx", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_Delete_InvalidGuid_ReturnsFailure()
    {
        var result = CommandParser.Parse("DELETE not-a-guid", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_Delete_UnknownGuid_ReturnsFailure()
    {
        var result = CommandParser.Parse($"DELETE {Guid.NewGuid()}", new ObservableCollection<Shape>());
        Assert.False(result.Success);
        Assert.Contains("Không tìm thấy shape", result.ErrorMessage);
    }

    [Fact]
    public void Parse_Undo_WithExtraArgs_ReturnsFailure()
    {
        var result = CommandParser.Parse("UNDO extra", new ObservableCollection<Shape>());
        Assert.False(result.Success);
    }

    [Fact]
    public void Parse_DoesNotThrow_OnGarbageInput()
    {
        var exception = Record.Exception(() => CommandParser.Parse(",,,@@@ ---", new ObservableCollection<Shape>()));
        Assert.Null(exception);
    }

    private static TCommand GetEditorCommand<TCommand>(CliParseResult result)
        where TCommand : IEditorCommand
    {
        var execute = Assert.IsType<ExecuteEditorCliCommand>(result.Command);
        return Assert.IsType<TCommand>(execute.EditorCommand);
    }
}
