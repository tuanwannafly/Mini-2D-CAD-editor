using CadEditor.Geometry.Utils;
using CadEditor.Models;

namespace CadEditor.Tests.Geometry;

public class GeometryUtilsTests
{
    private static void AssertPointEqual(Point2D expected, Point2D actual, int precision = 9)
    {
        Assert.Equal(expected.X, actual.X, precision);
        Assert.Equal(expected.Y, actual.Y, precision);
    }

    [Fact]
    public void LineIntersect_IntersectingSegments_ReturnsIntersectionPoint()
    {
        var lineA = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        var lineB = new LineShape(new Point2D(0, 10), new Point2D(10, 0));

        var result = GeometryUtils.LineIntersect(lineA, lineB);

        Assert.NotNull(result);
        AssertPointEqual(new Point2D(5, 5), result.Value);
    }

    [Fact]
    public void LineIntersect_ParallelLines_ReturnsNull()
    {
        var lineA = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        var lineB = new LineShape(new Point2D(0, 5), new Point2D(10, 15));

        var result = GeometryUtils.LineIntersect(lineA, lineB);

        Assert.Null(result);
    }

    [Fact]
    public void LineIntersect_OverlappingCollinearLines_ReturnsNull()
    {
        var lineA = new LineShape(new Point2D(0, 0), new Point2D(10, 10));
        var lineB = new LineShape(new Point2D(2, 2), new Point2D(8, 8));

        var result = GeometryUtils.LineIntersect(lineA, lineB);

        Assert.Null(result);
    }

    [Fact]
    public void LineIntersect_IntersectionOutsideSegmentBounds_ReturnsNull()
    {
        var lineA = new LineShape(new Point2D(0, 0), new Point2D(5, 5));
        var lineB = new LineShape(new Point2D(10, 0), new Point2D(10, 10));

        var result = GeometryUtils.LineIntersect(lineA, lineB);

        Assert.Null(result);
    }

    [Fact]
    public void LineIntersect_PerpendicularSegments_ReturnsCorrectIntersection()
    {
        var lineA = new LineShape(new Point2D(-5, 0), new Point2D(5, 0));
        var lineB = new LineShape(new Point2D(0, -5), new Point2D(0, 5));

        var result = GeometryUtils.LineIntersect(lineA, lineB);

        Assert.NotNull(result);
        AssertPointEqual(new Point2D(0, 0), result.Value);
    }

    [Fact]
    public void PointInPolygon_PointInsideConvexPolygon_ReturnsTrue()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });

        Assert.True(GeometryUtils.PointInPolygon(new Point2D(5, 5), polygon));
    }

    [Fact]
    public void PointInPolygon_PointOutsideConvexPolygon_ReturnsFalse()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });

        Assert.False(GeometryUtils.PointInPolygon(new Point2D(15, 5), polygon));
    }

    [Fact]
    public void PointInPolygon_PointOnBoundary_ReturnsTrue()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });

        Assert.True(GeometryUtils.PointInPolygon(new Point2D(5, 0), polygon));
    }

    [Fact]
    public void PointInPolygon_PointOnVertex_ReturnsTrue()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        });

        Assert.True(GeometryUtils.PointInPolygon(new Point2D(0, 0), polygon));
    }

    [Fact]
    public void PointInPolygon_PointInsideConcavePolygon_ReturnsTrue()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(5, 5),
            new Point2D(5, 10),
            new Point2D(0, 10)
        });

        Assert.True(GeometryUtils.PointInPolygon(new Point2D(3, 3), polygon));
    }

    [Fact]
    public void PointInPolygon_PointInConcaveNotch_ReturnsFalse()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 5),
            new Point2D(5, 5),
            new Point2D(5, 10),
            new Point2D(0, 10)
        });

        Assert.False(GeometryUtils.PointInPolygon(new Point2D(7, 7), polygon));
    }

    [Fact]
    public void PointInPolygon_DegeneratePolygon_ReturnsFalse()
    {
        var polygon = new PolygonShape(new[] { new Point2D(0, 0), new Point2D(1, 1) });
        Assert.False(GeometryUtils.PointInPolygon(new Point2D(0, 0), polygon));
    }

    [Fact]
    public void PointInPolygon_PointOutsideConcavePolygon_ReturnsFalse()
    {
        var polygon = new PolygonShape(new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(5, 5),
            new Point2D(0, 10)
        });

        Assert.False(GeometryUtils.PointInPolygon(new Point2D(12, 12), polygon));
    }

    [Fact]
    public void ConvexHull_ReturnsCorrectHullForSquareWithInteriorPoints()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10),
            new Point2D(5, 5),
            new Point2D(3, 3)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(4, hull.Count);
        Assert.Contains(new Point2D(0, 0), hull);
        Assert.Contains(new Point2D(10, 0), hull);
        Assert.Contains(new Point2D(10, 10), hull);
        Assert.Contains(new Point2D(0, 10), hull);
    }

    [Fact]
    public void ConvexHull_LessThanThreePoints_ReturnsInput()
    {
        var singlePoint = new[] { new Point2D(1, 2) };
        var hull1 = GeometryUtils.ConvexHull(singlePoint);
        Assert.Single(hull1);
        Assert.Equal(new Point2D(1, 2), hull1[0]);

        var twoPoints = new[] { new Point2D(1, 2), new Point2D(3, 4) };
        var hull2 = GeometryUtils.ConvexHull(twoPoints);
        Assert.Equal(2, hull2.Count);
    }

    [Fact]
    public void ConvexHull_EmptyPoints_ReturnsEmpty()
    {
        var hull = GeometryUtils.ConvexHull(Array.Empty<Point2D>());
        Assert.Empty(hull);
    }

    [Fact]
    public void ConvexHull_DuplicatePoints_Deduplicates()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10),
            new Point2D(0, 0),
            new Point2D(10, 0)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(4, hull.Count);
    }

    [Fact]
    public void ConvexHull_CollinearPoints_ReturnsTwoEndpoints()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(2, 2),
            new Point2D(5, 5),
            new Point2D(10, 10)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(2, hull.Count);
        Assert.Contains(new Point2D(0, 0), hull);
        Assert.Contains(new Point2D(10, 10), hull);
    }

    [Fact]
    public void ConvexHull_ThreePoints_ReturnsAllThree()
    {
        var points = new[] { new Point2D(0, 0), new Point2D(10, 0), new Point2D(5, 10) };
        var hull = GeometryUtils.ConvexHull(points);
        Assert.Equal(3, hull.Count);
        Assert.Contains(new Point2D(0, 0), hull);
        Assert.Contains(new Point2D(10, 0), hull);
        Assert.Contains(new Point2D(5, 10), hull);
    }

    [Fact]
    public void ConvexHull_AllPointsAlreadyConvex_ReturnsSameOrder()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(0, 10)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(4, hull.Count);
        AssertPointEqual(new Point2D(0, 0), hull[0]);
        AssertPointEqual(new Point2D(10, 0), hull[1]);
        AssertPointEqual(new Point2D(10, 10), hull[2]);
        AssertPointEqual(new Point2D(0, 10), hull[3]);
    }

    [Fact]
    public void ConvexHull_HullIsCounterClockwise()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(10, 10),
            new Point2D(5, 5),
            new Point2D(0, 10)
        };

        var hull = GeometryUtils.ConvexHull(points);

        for (int i = 0; i < hull.Count; i++)
        {
            var a = hull[i];
            var b = hull[(i + 1) % hull.Count];
            var c = hull[(i + 2) % hull.Count];

            double cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);
            Assert.True(cross >= -1e-10);
        }
    }

    [Fact]
    public void ConvexHull_CollinearDistinctDirections_ReturnsTwoEndpoints()
    {
        var points = new[]
        {
            new Point2D(-5, -5),
            new Point2D(-3, -3),
            new Point2D(0, 0),
            new Point2D(3, 3),
            new Point2D(5, 5)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(2, hull.Count);
        Assert.Contains(new Point2D(-5, -5), hull);
        Assert.Contains(new Point2D(5, 5), hull);
    }

    [Fact]
    public void ConvexHull_TriangleWithPointsOnEdge_ReturnsThreeVertices()
    {
        var points = new[]
        {
            new Point2D(0, 0),
            new Point2D(10, 0),
            new Point2D(5, 10),
            new Point2D(5, 0),
            new Point2D(2, 4)
        };

        var hull = GeometryUtils.ConvexHull(points);

        Assert.Equal(3, hull.Count);
        Assert.Contains(new Point2D(0, 0), hull);
        Assert.Contains(new Point2D(10, 0), hull);
        Assert.Contains(new Point2D(5, 10), hull);
    }
}
