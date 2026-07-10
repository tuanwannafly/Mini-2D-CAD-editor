using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CadEditor.Commands;
using CadEditor.Models;

namespace CadEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<Shape> Shapes { get; } = new();

    [ObservableProperty]
    private DrawingTool currentTool = DrawingTool.Select;

    [ObservableProperty]
    private Shape? previewShape;

    [ObservableProperty]
    private bool canUndo;

    [ObservableProperty]
    private bool canRedo;

    private readonly UndoRedoManager undoRedoManager = new();
    private readonly QuadTree quadTree;

    private Point2D dragStart;
    private PolygonShape? polygonInProgress;

    public MainViewModel()
    {
        // Use the full canvas size as the QuadTree bounds; expand dynamically if needed
        quadTree = new QuadTree(new BoundingBox(0, 0, 2000, 2000), capacity: 8, maxDepth: 10);

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

    public void OnCanvasMouseDown(Point2D point)
    {
        if (CurrentTool == DrawingTool.Select)
        {
            SelectShapeAt(point);
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
        if (CurrentTool == DrawingTool.Select)
            return;

        if (CurrentTool == DrawingTool.Polygon)
        {
            polygonInProgress?.UpdateLastVertex(point);
            return;
        }

        UpdatePreviewShape(point);
    }

    public void OnCanvasMouseUp(Point2D point)
    {
        if (CurrentTool == DrawingTool.Select)
            return;

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

    private void SelectShapeAt(Point2D point)
    {
        quadTree.Rebuild(Shapes);
        var candidates = quadTree.Query(point);

        Shape? best = null;
        int bestIndex = -1;
        var seen = new HashSet<Guid>();

        foreach (var candidate in candidates)
        {
            if (!seen.Add(candidate.Id))
                continue;

            if (!candidate.HitTest(point))
                continue;

            int index = Shapes.IndexOf(candidate);
            if (index > bestIndex)
            {
                bestIndex = index;
                best = candidate;
            }
        }

        foreach (var shape in Shapes)
            shape.IsSelected = shape == best;
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