using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CadEditor.Cli;
using CadEditor.Commands;
using CadEditor.Models;
using CadEditor.Services;

namespace CadEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<Shape> Shapes { get; } = new();

    public ObservableCollection<string> CliLog { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectToolActive))]
    [NotifyPropertyChangedFor(nameof(IsLineToolActive))]
    [NotifyPropertyChangedFor(nameof(IsCircleToolActive))]
    [NotifyPropertyChangedFor(nameof(IsRectToolActive))]
    [NotifyPropertyChangedFor(nameof(IsPolygonToolActive))]
    [NotifyPropertyChangedFor(nameof(IsMoveToolActive))]
    [NotifyPropertyChangedFor(nameof(CurrentToolLabel))]
    private DrawingTool currentTool = DrawingTool.Select;

    [ObservableProperty]
    private string cliInput = string.Empty;

    [ObservableProperty]
    private Shape? previewShape;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedShapeStatus))]
    private Shape? selectedShape;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MousePositionStatus))]
    private double mouseX;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MousePositionStatus))]
    private double mouseY;

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

    [ObservableProperty]
    private PersistenceFormat selectedPersistenceFormat = PersistenceFormat.Json;

    [ObservableProperty]
    private string persistencePath = "drawing.json";

    [ObservableProperty]
    private string persistenceStatus = string.Empty;

    public Array PersistenceFormats { get; } = Enum.GetValues(typeof(PersistenceFormat));

    public string MousePositionStatus => $"X: {MouseX:0.0}, Y: {MouseY:0.0}";

    public string SelectedShapeStatus => SelectedShape == null ? "None" : GetShapeDisplayName(SelectedShape);

    public string CurrentToolLabel => CurrentTool switch
    {
        DrawingTool.None => "None",
        _ => CurrentTool.ToString()
    };

    public bool IsSelectToolActive
    {
        get => CurrentTool == DrawingTool.Select;
        set { if (value) SelectToolCommand.Execute(DrawingTool.Select); }
    }

    public bool IsLineToolActive
    {
        get => CurrentTool == DrawingTool.Line;
        set { if (value) SelectToolCommand.Execute(DrawingTool.Line); }
    }

    public bool IsCircleToolActive
    {
        get => CurrentTool == DrawingTool.Circle;
        set { if (value) SelectToolCommand.Execute(DrawingTool.Circle); }
    }

    public bool IsRectToolActive
    {
        get => CurrentTool == DrawingTool.Rectangle;
        set { if (value) SelectToolCommand.Execute(DrawingTool.Rectangle); }
    }

    public bool IsPolygonToolActive
    {
        get => CurrentTool == DrawingTool.Polygon;
        set { if (value) SelectToolCommand.Execute(DrawingTool.Polygon); }
    }

    public bool IsMoveToolActive
    {
        get => CurrentTool == DrawingTool.None && SelectedShape != null;
        set { if (value) SelectToolCommand.Execute(DrawingTool.None); }
    }

    public ObservableCollection<TransformHandleInfo> TransformHandles { get; } = new();

    private readonly UndoRedoManager undoRedoManager = new();
    private readonly QuadTree quadTree;
    private const double HitTestPadding = 4;

    private Point2D dragStart;
    private Point2D moveStart;
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
    private void DeleteSelectedShape()
    {
        if (SelectedShape == null) return;

        undoRedoManager.ExecuteCommand(new DeleteShapeCommand(Shapes, SelectedShape));
        SelectedShape = null;
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

    [RelayCommand]
    private void SaveDrawing()
    {
        var service = PersistenceServiceFactory.Create(SelectedPersistenceFormat);
        service.Save(PersistencePath, Shapes);
        PersistenceStatus = $"Saved {Shapes.Count} shapes to {PersistencePath}.";
    }

    [RelayCommand]
    private void LoadDrawing()
    {
        var service = PersistenceServiceFactory.Create(SelectedPersistenceFormat);
        var loadedShapes = service.Load(PersistencePath);

        Shapes.Clear();
        foreach (var shape in loadedShapes)
        {
            Shapes.Add(shape);
        }

        SelectedShape = null;
        PreviewShape = null;
        TransformHandles.Clear();
        RefreshUndoRedoState();
        PersistenceStatus = $"Loaded {Shapes.Count} shapes from {PersistencePath}.";
    }

    partial void OnSelectedPersistenceFormatChanged(PersistenceFormat value)
    {
        string extension = value == PersistenceFormat.Sqlite ? ".db" : ".json";
        PersistencePath = Path.ChangeExtension(PersistencePath, extension) ?? $"drawing{extension}";
    }

    public void OnCanvasMouseDown(Point2D point)
    {
        UpdateMousePosition(point);
        point = SnapPoint(point);
        if (CurrentTool is DrawingTool.Select or DrawingTool.None)
        {
            var handle = HitTestHandle(point);
            if (handle != null)
            {
                StartTransform(point, handle.Value);
                return;
            }

            SelectedShape = HitTestShape(point);
            if (SelectedShape != null)
            {
                movingShape = SelectedShape;
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
        UpdateMousePosition(point);
        point = SnapPoint(point);
        if (activeHandle != null)
        {
            UpdateTransform(point);
            return;
        }

        if (movingShape != null)
        {
            var boundsBefore = movingShape.GetBounds();
            ShapeMover.MoveBy(movingShape, point.X - dragStart.X, point.Y - dragStart.Y);
            dragStart = point;
            if (movingShape.GetBounds() != boundsBefore)
                RebuildTransformHandles();
            return;
        }

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
        UpdateMousePosition(point);
        point = SnapPoint(point);
        if (activeHandle != null)
        {
            FinishTransform();
            return;
        }

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

    public void UpdateMousePosition(Point2D point)
    {
        MouseX = point.X;
        MouseY = point.Y;
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

    private Point2D SnapPoint(Point2D point)
    {
        IEnumerable<Point2D>? snapPoints = SnapToPointEnabled ? SnapService.GetSnapPoints(Shapes) : null;
        return SnapService.Snap(point, GridSize, SnapToGridEnabled, snapPoints, SnapService.PointSnapThreshold, SnapToPointEnabled);
    }

    private HandleType? HitTestHandle(Point2D point)
    {
        const double hitSize = 14;
        foreach (var h in TransformHandles)
        {
            if (Math.Abs(point.X - (h.X + 6)) < hitSize / 2 &&
                Math.Abs(point.Y - (h.Y + 6)) < hitSize / 2)
                return h.Type;
        }
        return null;
    }

    private Shape? HitTestShape(Point2D point)
    {
        quadTree.Rebuild(Shapes);
        var candidates = quadTree.Query(new BoundingBox(
            point.X - HitTestPadding,
            point.Y - HitTestPadding,
            point.X + HitTestPadding,
            point.Y + HitTestPadding));
        var seen = new HashSet<Guid>();

        foreach (var shape in candidates.AsEnumerable().Reverse())
        {
            if (!seen.Add(shape.Id))
                continue;

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

            if (shape.HitTest(local, HitTestPadding) ||
                bounds.MinX <= local.X && local.X <= bounds.MaxX &&
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

        shapeCenter = SelectedShape.GetCenter();
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

        // Use axis-aligned bounds (rotation/scale already applied) so the visual
        // handles wrap the shape exactly as it appears on screen.
        var bounds = SelectedShape.GetAxisAlignedBounds();
        var center = new Point2D(
            bounds.MinX + bounds.Width / 2,
            bounds.MinY + bounds.Height / 2);

        var corners = new[]
        {
            new Point2D(bounds.MinX, bounds.MinY),       // TL
            new Point2D(bounds.MaxX, bounds.MinY),       // TR
            new Point2D(bounds.MaxX, bounds.MaxY),       // BR
            new Point2D(bounds.MinX, bounds.MaxY)        // BL
        };

        var types = new[] { HandleType.TopLeft, HandleType.TopRight, HandleType.BottomRight, HandleType.BottomLeft };
        for (int i = 0; i < 4; i++)
            TransformHandles.Add(new TransformHandleInfo { X = corners[i].X - 6, Y = corners[i].Y - 6, Type = types[i] });

        var edgePairs = new[] { (0, 1), (1, 2), (2, 3), (3, 0) };
        var edgeTypes = new[] { HandleType.TopCenter, HandleType.MiddleRight, HandleType.BottomCenter, HandleType.MiddleLeft };
        for (int i = 0; i < 4; i++)
        {
            var (a, b) = edgePairs[i];
            TransformHandles.Add(new TransformHandleInfo
            {
                X = (corners[a].X + corners[b].X) / 2 - 6,
                Y = (corners[a].Y + corners[b].Y) / 2 - 6,
                Type = edgeTypes[i]
            });
        }

        // Rotation handle: outward from the top edge midpoint
        var topA = corners[0];
        var topB = corners[1];
        double edx = topB.X - topA.X;
        double edy = topB.Y - topA.Y;
        double elen = Math.Sqrt(edx * edx + edy * edy);
        if (elen > 0)
        {
            double rhx = (topA.X + topB.X) / 2 + (-edy / elen) * 28;
            double rhy = (topA.Y + topB.Y) / 2 + (edx / elen) * 28;
            TransformHandles.Add(new TransformHandleInfo { X = rhx - 6, Y = rhy - 6, Type = HandleType.Rotation });

            RotationLineX1 = (topA.X + topB.X) / 2;
            RotationLineY1 = (topA.Y + topB.Y) / 2;
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

    private static string GetShapeDisplayName(Shape shape) => shape switch
    {
        LineShape => "Line",
        CircleShape => "Circle",
        RectangleShape => "Rectangle",
        PolygonShape => "Polygon",
        ArcShape => "Arc",
        _ => shape.GetType().Name
    };
}
