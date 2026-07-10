using CadEditor.Models;

namespace CadEditor.Geometry.Utils;

public static class GeometryUtils
{
    private const double Epsilon = 1e-10;

    public static Point2D? LineIntersect(LineShape a, LineShape b)
    {
        double d1x = a.End.X - a.Start.X;
        double d1y = a.End.Y - a.Start.Y;
        double d2x = b.End.X - b.Start.X;
        double d2y = b.End.Y - b.Start.Y;

        double denom = d1x * d2y - d1y * d2x;

        if (Math.Abs(denom) < Epsilon)
            return null;

        double dx = b.Start.X - a.Start.X;
        double dy = b.Start.Y - a.Start.Y;

        double t = (dx * d2y - dy * d2x) / denom;
        double u = (dx * d1y - dy * d1x) / denom;

        if (t < 0 || t > 1 || u < 0 || u > 1)
            return null;

        return new Point2D(a.Start.X + t * d1x, a.Start.Y + t * d1y);
    }

    public static bool PointInPolygon(Point2D point, PolygonShape polygon)
    {
        var vertices = polygon.Vertices;
        if (vertices.Count < 3)
            return false;

        bool inside = false;
        int n = vertices.Count;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var vi = vertices[i];
            var vj = vertices[j];

            if (IsPointOnSegment(point, vi, vj))
                return true;

            if ((vi.Y > point.Y) != (vj.Y > point.Y) &&
                point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public static IReadOnlyList<Point2D> ConvexHull(IEnumerable<Point2D> points)
    {
        var pts = points.Distinct().ToList();
        if (pts.Count <= 2)
            return pts;

        var pivot = pts[0];
        for (int i = 1; i < pts.Count; i++)
        {
            if (pts[i].Y < pivot.Y - Epsilon ||
                (Math.Abs(pts[i].Y - pivot.Y) < Epsilon && pts[i].X < pivot.X - Epsilon))
            {
                pivot = pts[i];
            }
        }

        pts.Sort((p1, p2) =>
        {
            if (p1.Equals(pivot)) return -1;
            if (p2.Equals(pivot)) return 1;

            double cross = Cross(pivot, p1, p2);
            if (Math.Abs(cross) > Epsilon)
                return cross > 0 ? -1 : 1;

            double d1 = DistanceSquared(pivot, p1);
            double d2 = DistanceSquared(pivot, p2);
            return d1.CompareTo(d2);
        });

        var unique = new List<Point2D> { pts[0], pts[1] };
        for (int i = 2; i < pts.Count; i++)
        {
            double cross = Cross(pivot, unique[^1], pts[i]);
            if (Math.Abs(cross) > Epsilon)
                unique.Add(pts[i]);
            else
                unique[^1] = pts[i];
        }

        if (unique.Count <= 2)
            return unique;

        var stack = new List<Point2D> { unique[0], unique[1], unique[2] };
        for (int i = 3; i < unique.Count; i++)
        {
            while (stack.Count > 1)
            {
                var top = stack[^1];
                var second = stack[^2];
                double turn = Cross(second, top, unique[i]);
                if (turn <= Epsilon)
                    stack.RemoveAt(stack.Count - 1);
                else
                    break;
            }
            stack.Add(unique[i]);
        }

        return stack;
    }

    private static double Cross(Point2D a, Point2D b, Point2D c)
    {
        return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }

    private static double DistanceSquared(Point2D a, Point2D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        return dx * dx + dy * dy;
    }

    private static bool IsPointOnSegment(Point2D p, Point2D a, Point2D b)
    {
        double cross = (p.X - a.X) * (b.Y - a.Y) - (p.Y - a.Y) * (b.X - a.X);
        if (Math.Abs(cross) > Epsilon)
            return false;

        double dot = (p.X - a.X) * (b.X - a.X) + (p.Y - a.Y) * (b.Y - a.Y);
        if (dot < -Epsilon)
            return false;

        double lenSq = (b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y);
        if (dot > lenSq + Epsilon)
            return false;

        return true;
    }
}
