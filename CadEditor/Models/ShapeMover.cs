namespace CadEditor.Models;

public static class ShapeMover
{
    public static Point2D GetPosition(Shape shape)
    {
        var bounds = shape.GetBounds();
        return new Point2D(bounds.MinX, bounds.MinY);
    }

    public static void MoveTo(Shape shape, Point2D position)
    {
        var current = GetPosition(shape);
        MoveBy(shape, position.X - current.X, position.Y - current.Y);
    }

    public static void MoveBy(Shape shape, double dx, double dy)
    {
        switch (shape)
        {
            case LineShape line:
                line.Start = MovePoint(line.Start, dx, dy);
                line.End = MovePoint(line.End, dx, dy);
                break;
            case CircleShape circle:
                circle.Center = MovePoint(circle.Center, dx, dy);
                break;
            case RectangleShape rectangle:
                rectangle.TopLeft = MovePoint(rectangle.TopLeft, dx, dy);
                break;
            case PolygonShape polygon:
                polygon.Vertices = polygon.Vertices.Select(point => MovePoint(point, dx, dy)).ToList();
                break;
            case ArcShape arc:
                arc.Center = MovePoint(arc.Center, dx, dy);
                break;
        }
    }

    private static Point2D MovePoint(Point2D point, double dx, double dy) =>
        new(point.X + dx, point.Y + dy);
}
