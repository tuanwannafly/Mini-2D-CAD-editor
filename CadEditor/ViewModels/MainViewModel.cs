using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CadEditor.Cli;
using CadEditor.Commands;
using CadEditor.Models;

namespace CadEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<Shape> Shapes { get; } = new();

    public ObservableCollection<string> CliLog { get; } = new();

    [ObservableProperty]
    private DrawingTool currentTool = DrawingTool.Line;

    [ObservableProperty]
    private string cliInput = string.Empty;

    [ObservableProperty]
    private Shape? previewShape;

    [ObservableProperty]
    private bool canUndo;

    [ObservableProperty]
    private bool canRedo;

    private readonly UndoRedoManager undoRedoManager = new();
    private const double HitTestPadding = 4;

    private Point2D dragStart;
    private Point2D moveStart;
    private Shape? selectedShape;
    private Shape? movingShape;
    private PolygonShape? polygonInProgress;

    public MainViewModel()
    {
        Shapes.Add(new LineShape(new Point2D(20, 20), new Point2D(200, 150)));
        Shapes.Add(new CircleShape(new Point2D(300, 100), 60));
        Shapes.Add(new RectangleShape(new Point2D(400, 50), 120, 80));
        Shapes.Add(new PolygonShape(new[] { new Point2D(500, 200), new Point2D(600, 200), new Point2D(550, 280) }));
        Shapes.Add(new ArcShape(new Point2D(700, 150), 50, 0, 180));
    }

    [RelayCommand]
    private void SelectTool(DrawingTool tool)
    {
        CancelPolygon();
        PreviewShape = null;
        CurrentTool = tool;
    }

    [RelayCommand]
    private void Undo()
    {
        undoRedoManager.Undo();
        RefreshUndoRedoState();
    }

    [RelayCommand]
    private void Redo()
    {
        undoRedoManager.Redo();
        RefreshUndoRedoState();
    }

    [RelayCommand]
    private void SubmitCliInput()
    {
        var input = CliInput.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        CliLog.Add($"> {input}");
        CliLog.Add(ExecuteCliInput(input));
        CliInput = string.Empty;
    }

    /// <summary>
    /// Parse và thực thi 1 dòng lệnh CLI. Trả về message để log ra UI (US-2.5).
    /// </summary>
    public string ExecuteCliInput(string input)
    {
        var result = CommandParser.Parse(input, Shapes);
        if (!result.Success)
            return result.ErrorMessage!;

        switch (result.Command)
        {
            case ExecuteEditorCliCommand execute:
                undoRedoManager.ExecuteCommand(execute.EditorCommand);
                RefreshUndoRedoState();
                return "Đã thực thi lệnh.";

            case UndoCliCommand:
                if (!undoRedoManager.CanUndo) return "Không có gì để Undo.";
                undoRedoManager.Undo();
                RefreshUndoRedoState();
                return "Đã Undo.";

            case RedoCliCommand:
                if (!undoRedoManager.CanRedo) return "Không có gì để Redo.";
                undoRedoManager.Redo();
                RefreshUndoRedoState();
                return "Đã Redo.";

            default:
                return "Lệnh không được hỗ trợ.";
        }
    }

    public void OnCanvasMouseDown(Point2D point)
    {
        if (CurrentTool == DrawingTool.None)
        {
            SelectShapeAt(point);
            if (selectedShape != null)
            {
                movingShape = selectedShape;
                dragStart = point;
                moveStart = ShapeMover.GetPosition(movingShape);
            }
            return;
        }

        if (CurrentTool == DrawingTool.Polygon)
        {
            if (polygonInProgress == null)
            {
                polygonInProgress = new PolygonShape(new[] { point, point });
                PreviewShape = polygonInProgress;
            }
            else
            {
                polygonInProgress.AddVertex(point);
            }
            return;
        }

        dragStart = point;
        PreviewShape = CurrentTool switch
        {
            DrawingTool.Line => new LineShape(point, point),
            DrawingTool.Circle => new CircleShape(point, 0),
            DrawingTool.Rectangle => new RectangleShape(point, 0, 0),
            _ => null
        };
    }

    public void OnCanvasMouseMove(Point2D point)
    {
        if (movingShape != null)
        {
            ShapeMover.MoveBy(movingShape, point.X - dragStart.X, point.Y - dragStart.Y);
            dragStart = point;
            return;
        }

        if (CurrentTool == DrawingTool.Polygon)
        {
            polygonInProgress?.UpdateLastVertex(point);
            return;
        }

        UpdatePreviewShape(point);
    }

    public void OnCanvasMouseUp(Point2D point)
    {
        if (movingShape != null)
        {
            var shape = movingShape;
            movingShape = null;
            var moveEnd = ShapeMover.GetPosition(shape);
            if (moveEnd != moveStart)
            {
                undoRedoManager.ExecuteCommand(new MoveShapeCommand(shape, moveStart, moveEnd));
                RefreshUndoRedoState();
            }
            return;
        }

        if (CurrentTool == DrawingTool.Polygon)
            return;

        if (PreviewShape == null) return;

        UpdatePreviewShape(point);

        bool isDegenerate = PreviewShape switch
        {
            LineShape line => Distance(line.Start, line.End) < 1,
            CircleShape circle => circle.Radius < 1,
            RectangleShape rect => rect.Width < 1 || rect.Height < 1,
            _ => false
        };

        if (!isDegenerate)
        {
            undoRedoManager.ExecuteCommand(new AddShapeCommand(Shapes, PreviewShape));
            RefreshUndoRedoState();
        }

        PreviewShape = null;
    }

    public void FinishPolygon()
    {
        if (polygonInProgress == null) return;

        polygonInProgress.RemoveLastVertex();

        if (polygonInProgress.Vertices.Count >= 3)
        {
            undoRedoManager.ExecuteCommand(new AddShapeCommand(Shapes, polygonInProgress));
            RefreshUndoRedoState();
        }

        polygonInProgress = null;
        PreviewShape = null;
    }

    public void CancelPolygon()
    {
        polygonInProgress = null;
        PreviewShape = null;
    }

    private void UpdatePreviewShape(Point2D point)
    {
        switch (PreviewShape)
        {
            case LineShape line:
                line.End = point;
                break;
            case CircleShape circle:
                circle.Radius = Distance(circle.Center, point);
                break;
            case RectangleShape rect:
                var (topLeft, width, height) = NormalizeRect(dragStart, point);
                rect.TopLeft = topLeft;
                rect.Width = width;
                rect.Height = height;
                break;
        }
    }

    private void SelectShapeAt(Point2D point)
    {
        var hit = Shapes.Reverse().FirstOrDefault(shape => ContainsPoint(shape.GetBounds(), point));

        foreach (var shape in Shapes)
            shape.IsSelected = false;

        selectedShape = hit;

        if (selectedShape != null)
            selectedShape.IsSelected = true;
    }

    private static bool ContainsPoint(BoundingBox bounds, Point2D point) =>
        point.X >= bounds.MinX - HitTestPadding &&
        point.X <= bounds.MaxX + HitTestPadding &&
        point.Y >= bounds.MinY - HitTestPadding &&
        point.Y <= bounds.MaxY + HitTestPadding;

    private void RefreshUndoRedoState()
    {
        CanUndo = undoRedoManager.CanUndo;
        CanRedo = undoRedoManager.CanRedo;
    }

    private static double Distance(Point2D a, Point2D b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static (Point2D topLeft, double width, double height) NormalizeRect(Point2D start, Point2D current)
    {
        double x = Math.Min(start.X, current.X);
        double y = Math.Min(start.Y, current.Y);
        double w = Math.Abs(current.X - start.X);
        double h = Math.Abs(current.Y - start.Y);
        return (new Point2D(x, y), w, h);
    }
}
