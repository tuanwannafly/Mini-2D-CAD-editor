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
    private DrawingTool currentTool = DrawingTool.Select;

    [ObservableProperty]
    private string cliInput = string.Empty;

    [ObservableProperty]
    private Shape? previewShape;

    [ObservableProperty]
    private Shape? selectedShape;

    [ObservableProperty]
    private double rotationLineX1;

    [ObservableProperty]
    private double rotationLineY1;

    [ObservableProperty]
    private double rotationLineX2;

    [ObservableProperty]
    private double rotationLineY2;

    [ObservableProperty]
    private bool canUndo;

    [ObservableProperty]
    private bool canRedo;

    [ObservableProperty]
    private bool snapToGridEnabled;

    [ObservableProperty]
    private bool snapToPointEnabled;

    [ObservableProperty]
    private double gridSize = SnapService.DefaultGridSize;
    public ObservableCollection<TransformHandleInfo> TransformHandles { get; } = new();

    private readonly UndoRedoManager undoRedoManager = new();
    private readonly QuadTree quadTree;
    private const double HitTestPadding = 4;

    private Point2D dragStart;
    private Point2D moveStart;
    private Shape? selectedShape;
    private Shape? movingShape;
    private PolygonShape? polygonInProgress;

    // Transform drag state
    private HandleType? activeHandle;
    private Point2D transformStartPoint;
    private Point2D shapeCenter;
    private double initialRotationDeg;
    private double initialScaleX;
    private double initialScaleY;

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

    partial void OnCurrentToolChanged(DrawingTool value)
    {
        if (value != DrawingTool.Select)
        {
            SelectedShape = null;
            TransformHandles.Clear();
        }
    }

    partial void OnSelectedShapeChanged(Shape? value)
    {
        foreach (var s in Shapes)
            s.IsSelected = s == value;
        RebuildTransformHandles();
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
        RebuildTransformHandles();
    }

    [RelayCommand]
    private void Redo()
    {
        undoRedoManager.Redo();
        RefreshUndoRedoState();
        RebuildTransformHandles();
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
        point = SnapPoint(point);
        if (CurrentTool == DrawingTool.Select)
        {
            SelectShapeAt(point);
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
        point = SnapPoint(point);
        if (CurrentTool == DrawingTool.Select)
            return;
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
        point = SnapPoint(point);
        if (CurrentTool == DrawingTool.Select)
            return;
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

    private Point2D SnapPoint(Point2D point)
    {
        IEnumerable<Point2D>? snapPoints = SnapToPointEnabled ? SnapService.GetSnapPoints(Shapes) : null;
        return SnapService.Snap(point, GridSize, SnapToGridEnabled, snapPoints, SnapService.PointSnapThreshold, SnapToPointEnabled);
    private HandleType? HitTestHandle(Point2D point)
    {
        const double hitSize = 14;
        foreach (var h in TransformHandles)
        {
            if (Math.Abs(point.X - (h.X + 5)) < hitSize / 2 &&
                Math.Abs(point.Y - (h.Y + 5)) < hitSize / 2)
                return h.Type;
        }
        return null;
    }

    private Shape? HitTestShape(Point2D point)
    {
        foreach (var shape in Shapes.Reverse())
        {
            var bounds = shape.GetBounds();
            var cx = bounds.MinX + bounds.Width / 2;
            var cy = bounds.MinY + bounds.Height / 2;

            double dx = point.X - cx;
            double dy = point.Y - cy;

            double rad = -shape.RotationDeg * Math.PI / 180;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            double rx = dx * cos - dy * sin;
            double ry = dx * sin + dy * cos;

            double sx = shape.ScaleX == 0 ? 0 : rx / shape.ScaleX;
            double sy = shape.ScaleY == 0 ? 0 : ry / shape.ScaleY;

            var local = new Point2D(cx + sx, cy + sy);

            if (bounds.MinX <= local.X && local.X <= bounds.MaxX &&
                bounds.MinY <= local.Y && local.Y <= bounds.MaxY)
                return shape;
        }
        return null;
    }

    private void StartTransform(Point2D point, HandleType handleType)
    {
        if (SelectedShape == null) return;

        activeHandle = handleType;
        transformStartPoint = point;
        initialRotationDeg = SelectedShape.RotationDeg;
        initialScaleX = SelectedShape.ScaleX;
        initialScaleY = SelectedShape.ScaleY;

        var bounds = SelectedShape.GetBounds();
        shapeCenter = new Point2D(bounds.MinX + bounds.Width / 2, bounds.MinY + bounds.Height / 2);
    }

    private void UpdateTransform(Point2D point)
    {
        if (SelectedShape == null || activeHandle == null) return;

        if (activeHandle == HandleType.Rotation)
        {
            double startAngle = Math.Atan2(
                transformStartPoint.Y - shapeCenter.Y,
                transformStartPoint.X - shapeCenter.X) * 180 / Math.PI;
            double currentAngle = Math.Atan2(
                point.Y - shapeCenter.Y,
                point.X - shapeCenter.X) * 180 / Math.PI;
            SelectedShape.RotationDeg = initialRotationDeg + (currentAngle - startAngle);
        }
        else
        {
            var startLocal = RotatePoint(transformStartPoint, shapeCenter, -initialRotationDeg);
            var currentLocal = RotatePoint(point, shapeCenter, -initialRotationDeg);
            ApplyScaleFromHandle(startLocal, currentLocal);
        }

        RebuildTransformHandles();
    }

    private void FinishTransform()
    {
        if (SelectedShape == null || activeHandle == null) return;

        var cmd = new TransformCommand(
            SelectedShape,
            initialRotationDeg, initialScaleX, initialScaleY,
            SelectedShape.RotationDeg, SelectedShape.ScaleX, SelectedShape.ScaleY);

        undoRedoManager.ExecuteCommand(cmd);
        RefreshUndoRedoState();

        activeHandle = null;
    }

    private void RebuildTransformHandles()
    {
        TransformHandles.Clear();
        RotationLineX1 = 0;
        RotationLineY1 = 0;
        RotationLineX2 = 0;
        RotationLineY2 = 0;
        if (SelectedShape == null) return;

        var bounds = SelectedShape.GetBounds();
        var center = new Point2D(
            bounds.MinX + bounds.Width / 2,
            bounds.MinY + bounds.Height / 2);

        var raw = new[]
        {
            new Point2D(bounds.MinX, bounds.MinY),       // TL
            new Point2D(bounds.MaxX, bounds.MinY),       // TR
            new Point2D(bounds.MaxX, bounds.MaxY),       // BR
            new Point2D(bounds.MinX, bounds.MaxY)        // BL
        };

        var scaled = raw.Select(p => ScalePoint(p, center, SelectedShape.ScaleX, SelectedShape.ScaleY)).ToArray();
        var transformed = scaled.Select(p => RotatePoint(p, center, SelectedShape.RotationDeg)).ToArray();

        var types = new[] { HandleType.TopLeft, HandleType.TopRight, HandleType.BottomRight, HandleType.BottomLeft };
        for (int i = 0; i < 4; i++)
            TransformHandles.Add(new TransformHandleInfo { X = transformed[i].X - 5, Y = transformed[i].Y - 5, Type = types[i] });

        var edgePairs = new[] { (0, 1), (1, 2), (2, 3), (3, 0) };
        var edgeTypes = new[] { HandleType.TopCenter, HandleType.MiddleRight, HandleType.BottomCenter, HandleType.MiddleLeft };
        for (int i = 0; i < 4; i++)
        {
            var (a, b) = edgePairs[i];
            TransformHandles.Add(new TransformHandleInfo
            {
                X = (transformed[a].X + transformed[b].X) / 2 - 5,
                Y = (transformed[a].Y + transformed[b].Y) / 2 - 5,
                Type = edgeTypes[i]
            });
        }

        // Rotation handle: perpendicular outward from top edge
        var topEdge = (transformed[0], transformed[1]);
        double edx = topEdge.Item2.X - topEdge.Item1.X;
        double edy = topEdge.Item2.Y - topEdge.Item1.Y;
        double elen = Math.Sqrt(edx * edx + edy * edy);
        if (elen > 0)
        {
            double rhx = (topEdge.Item1.X + topEdge.Item2.X) / 2 + (-edy / elen) * 28;
            double rhy = (topEdge.Item1.Y + topEdge.Item2.Y) / 2 + (edx / elen) * 28;
            TransformHandles.Add(new TransformHandleInfo { X = rhx - 5, Y = rhy - 5, Type = HandleType.Rotation });

            RotationLineX1 = (topEdge.Item1.X + topEdge.Item2.X) / 2;
            RotationLineY1 = (topEdge.Item1.Y + topEdge.Item2.Y) / 2;
            RotationLineX2 = rhx;
            RotationLineY2 = rhy;
        }
    }

    private static Point2D RotatePoint(Point2D p, Point2D center, double deg)
    {
        double rad = deg * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double dx = p.X - center.X;
        double dy = p.Y - center.Y;
        return new Point2D(center.X + dx * cos - dy * sin, center.Y + dx * sin + dy * cos);
    }

    private static Point2D ScalePoint(Point2D p, Point2D center, double sx, double sy)
    {
        return new Point2D(center.X + (p.X - center.X) * sx, center.Y + (p.Y - center.Y) * sy);
    }

    private void ApplyScaleFromHandle(Point2D startLocal, Point2D currentLocal)
    {
        if (SelectedShape == null || activeHandle == null) return;

        bool scaleX = activeHandle is HandleType.TopLeft or HandleType.TopRight or
            HandleType.MiddleLeft or HandleType.MiddleRight or
            HandleType.BottomLeft or HandleType.BottomRight;
        bool scaleY = activeHandle is HandleType.TopLeft or HandleType.TopCenter or
            HandleType.TopRight or HandleType.BottomLeft or
            HandleType.BottomCenter or HandleType.BottomRight;

        if (scaleX)
        {
            double startDx = Math.Abs(startLocal.X - shapeCenter.X);
            if (startDx >= 1)
            {
                double currentDx = Math.Abs(currentLocal.X - shapeCenter.X);
                SelectedShape.ScaleX = Math.Max(0.05, initialScaleX * currentDx / startDx);
            }
        }

        if (scaleY)
        {
            double startDy = Math.Abs(startLocal.Y - shapeCenter.Y);
            if (startDy >= 1)
            {
                double currentDy = Math.Abs(currentLocal.Y - shapeCenter.Y);
                SelectedShape.ScaleY = Math.Max(0.05, initialScaleY * currentDy / startDy);
            }
        }
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
