using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CadEditor.Models;

namespace CadEditor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<Shape> Shapes { get; } = new();

    [ObservableProperty]
    private DrawingTool currentTool = DrawingTool.Line;

    private Shape? shapeInProgress;
    private Point2D dragStart;
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
        shapeInProgress = null;
        CurrentTool = tool;
    }

    public void OnCanvasMouseDown(Point2D point)
    {
        if (CurrentTool == DrawingTool.Polygon)
        {
            if (polygonInProgress == null)
            {
                // vertex[0] = điểm chốt, vertex[1] = rubber-band bám chuột
                polygonInProgress = new PolygonShape(new[] { point, point });
                Shapes.Add(polygonInProgress);
            }
            else
            {
                // chốt vertex rubber-band hiện tại, mở thêm 1 rubber-band mới
                polygonInProgress.AddVertex(point);
            }
            return;
        }

        dragStart = point;
        shapeInProgress = CurrentTool switch
        {
            DrawingTool.Line => new LineShape(point, point),
            DrawingTool.Circle => new CircleShape(point, 0),
            DrawingTool.Rectangle => new RectangleShape(point, 0, 0),
            _ => null
        };

        if (shapeInProgress != null)
            Shapes.Add(shapeInProgress);
    }

    public void OnCanvasMouseMove(Point2D point)
    {
        if (CurrentTool == DrawingTool.Polygon)
        {
            polygonInProgress?.UpdateLastVertex(point);
            return;
        }

        UpdateShapeInProgress(point);
    }

    public void OnCanvasMouseUp(Point2D point)
    {
        if (CurrentTool == DrawingTool.Polygon)
            return; // polygon commit qua click, không qua mouse-up

        if (shapeInProgress == null) return;

        UpdateShapeInProgress(point);

        // click không kéo -> shape rỗng -> bỏ, không add vào collection
        bool isDegenerate = shapeInProgress switch
        {
            LineShape line => Distance(line.Start, line.End) < 1,
            CircleShape circle => circle.Radius < 1,
            RectangleShape rect => rect.Width < 1 || rect.Height < 1,
            _ => false
        };

        if (isDegenerate)
            Shapes.Remove(shapeInProgress);

        shapeInProgress = null;
    }

    public void FinishPolygon()
    {
        if (polygonInProgress == null) return;

        polygonInProgress.RemoveLastVertex(); // bỏ rubber-band chưa chốt

        if (polygonInProgress.Vertices.Count < 3)
            Shapes.Remove(polygonInProgress);

        polygonInProgress = null;
    }

    public void CancelPolygon()
    {
        if (polygonInProgress == null) return;
        Shapes.Remove(polygonInProgress);
        polygonInProgress = null;
    }

    private void UpdateShapeInProgress(Point2D point)
    {
        switch (shapeInProgress)
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