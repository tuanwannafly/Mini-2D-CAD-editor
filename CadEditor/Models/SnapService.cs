namespace CadEditor.Models;

public static class SnapService
{
    public const double DefaultGridSize = 10.0;
    public const double PointSnapThreshold = 10.0;

    public static Point2D SnapToGrid(Point2D point, double gridSize)
    {
        if (gridSize <= 0)
            return point;

        double snappedX = Math.Round(point.X / gridSize, MidpointRounding.AwayFromZero) * gridSize;
        double snappedY = Math.Round(point.Y / gridSize, MidpointRounding.AwayFromZero) * gridSize;
        return new Point2D(snappedX, snappedY);
    }

    public static Point2D SnapToPoint(Point2D point, IEnumerable<Point2D> snapPoints, double threshold)
    {
        Point2D closest = point;
        double minDist = double.PositiveInfinity;

        foreach (var snapPoint in snapPoints)
        {
            double dx = point.X - snapPoint.X;
            double dy = point.Y - snapPoint.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist <= threshold && dist < minDist)
            {
                minDist = dist;
                closest = snapPoint;
            }
        }

        return closest;
    }

    public static Point2D Snap(Point2D point, double gridSize, bool snapToGrid, IEnumerable<Point2D>? snapPoints, double pointThreshold, bool snapToPoint)
    {
        Point2D result = point;

        if (snapToGrid)
            result = SnapToGrid(result, gridSize);

        if (snapToPoint && snapPoints != null)
            result = SnapToPoint(result, snapPoints, pointThreshold);

        return result;
    }

    public static IEnumerable<Point2D> GetSnapPoints(IEnumerable<Shape> shapes)
    {
        foreach (var shape in shapes)
        {
            switch (shape)
            {
                case LineShape line:
                    yield return line.Start;
                    yield return line.End;
                    break;
                case CircleShape circle:
                    yield return circle.Center;
                    break;
                case RectangleShape rect:
                    yield return rect.TopLeft;
                    yield return new Point2D(rect.TopLeft.X + rect.Width, rect.TopLeft.Y);
                    yield return new Point2D(rect.TopLeft.X + rect.Width, rect.TopLeft.Y + rect.Height);
                    yield return new Point2D(rect.TopLeft.X, rect.TopLeft.Y + rect.Height);
                    break;
                case PolygonShape polygon:
                    foreach (var v in polygon.Vertices)
                        yield return v;
                    break;
                case ArcShape arc:
                    yield return arc.Center;
                    break;
            }
        }
    }
}
