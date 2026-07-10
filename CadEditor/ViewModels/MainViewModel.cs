using System.Collections.ObjectModel;
using CadEditor.Models;

namespace CadEditor.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ObservableCollection<Shape> Shapes { get; } = new();

    public MainViewModel()
    {
        Shapes.Add(new LineShape(new Point2D(20, 20), new Point2D(200, 150)));
        Shapes.Add(new CircleShape(new Point2D(300, 100), 60));
        Shapes.Add(new RectangleShape(new Point2D(400, 50), 120, 80));
        Shapes.Add(new PolygonShape(new[] { new Point2D(500, 200), new Point2D(600, 200), new Point2D(550, 280) }));
        Shapes.Add(new ArcShape(new Point2D(700, 150), 50, 0, 180));
    }
}