using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class QuadTreeTests
{
    private static QuadTree CreateTree() =>
        new(new BoundingBox(0, 0, 1000, 1000), capacity: 4, maxDepth: 8);

    [Fact]
    public void Query_EmptyTree_ReturnsEmpty()
    {
        var tree = CreateTree();
        var results = tree.Query(new Point2D(50, 50));
        Assert.Empty(results);
    }

    [Fact]
    public void InsertAndQuery_PointWithinBounds_FindsShape()
    {
        var tree = CreateTree();
        var shape = new CircleShape(new Point2D(100, 100), 20);
        tree.Insert(shape);

        var results = tree.Query(new Point2D(100, 100));
        Assert.Contains(shape, results);
    }

    [Fact]
    public void InsertAndQuery_PointOutsideBounds_ReturnsNothing()
    {
        var tree = CreateTree();
        var shape = new CircleShape(new Point2D(100, 100), 10);
        tree.Insert(shape);

        var results = tree.Query(new Point2D(500, 500));
        Assert.DoesNotContain(shape, results);
    }

    [Fact]
    public void QueryByBoundingBox_ReturnsShapesInRegion()
    {
        var tree = CreateTree();
        var shape = new CircleShape(new Point2D(100, 100), 10);
        tree.Insert(shape);

        var results = tree.Query(new BoundingBox(50, 50, 150, 150));
        Assert.Contains(shape, results);
    }

    [Fact]
    public void QueryByBoundingBox_ExcludesShapesOutside()
    {
        var tree = CreateTree();
        var shape = new CircleShape(new Point2D(500, 500), 10);
        tree.Insert(shape);

        var results = tree.Query(new BoundingBox(0, 0, 100, 100));
        Assert.Empty(results);
    }

    [Fact]
    public void Rebuild_ReplacesAllShapes()
    {
        var tree = CreateTree();
        tree.Insert(new CircleShape(new Point2D(100, 100), 10));

        var newShapes = new List<Shape>
        {
            new RectangleShape(new Point2D(200, 200), 30, 40)
        };
        tree.Rebuild(newShapes);

        var results = tree.Query(new Point2D(100, 100));
        Assert.Empty(results);

        results = tree.Query(new Point2D(215, 220));
        Assert.Single(results);
    }

    [Fact]
    public void Clear_RemovesAllShapes()
    {
        var tree = CreateTree();
        tree.Insert(new CircleShape(new Point2D(100, 100), 10));
        tree.Clear();

        var results = tree.Query(new Point2D(100, 100));
        Assert.Empty(results);
    }

    [Fact]
    public void Insert_ManyShapes_AllAreQueryable()
    {
        var tree = CreateTree();
        var shapes = new List<Shape>();

        for (int i = 0; i < 100; i++)
        {
            var s = new CircleShape(new Point2D(i * 10, i * 10), 3);
            shapes.Add(s);
            tree.Insert(s);
        }

        foreach (var shape in shapes)
        {
            var results = tree.Query(shape.GetBounds());
            Assert.Contains(shape, results);
        }
    }

    [Fact]
    public void Insert_ShapesSpanningMultipleQuadrants_FoundFromEach()
    {
        var tree = CreateTree();
        // Large shape spanning all 4 quadrants of a subdivided tree
        var big = new RectangleShape(new Point2D(100, 100), 800, 800);
        tree.Insert(big);

        // Should be found from queries in each quadrant
        Assert.Contains(big, tree.Query(new Point2D(150, 150)));
        Assert.Contains(big, tree.Query(new Point2D(600, 150)));
        Assert.Contains(big, tree.Query(new Point2D(150, 600)));
        Assert.Contains(big, tree.Query(new Point2D(600, 600)));
    }

    [Fact]
    public void HitTest_Circle_DetectsClickOnEdge()
    {
        var shape = new CircleShape(new Point2D(100, 100), 20);

        Assert.True(shape.HitTest(new Point2D(100, 80)));  // top edge
        Assert.True(shape.HitTest(new Point2D(120, 100))); // right edge
        Assert.False(shape.HitTest(new Point2D(100, 50))); // far from edge
        Assert.True(shape.HitTest(new Point2D(100, 81)));  // within tolerance
    }

    [Fact]
    public void HitTest_Line_DetectsClickNearSegment()
    {
        var shape = new LineShape(new Point2D(0, 0), new Point2D(100, 0));

        Assert.True(shape.HitTest(new Point2D(50, 2)));   // near midpoint
        Assert.False(shape.HitTest(new Point2D(50, 10))); // too far
        Assert.True(shape.HitTest(new Point2D(0, 0)));    // at start
        Assert.True(shape.HitTest(new Point2D(100, 0)));  // at end
    }

    [Fact]
    public void HitTest_Rectangle_DetectsClickOnEdge()
    {
        var shape = new RectangleShape(new Point2D(10, 10), 100, 50);

        Assert.True(shape.HitTest(new Point2D(10, 10)));     // top-left corner
        Assert.True(shape.HitTest(new Point2D(60, 12)));     // near top edge
        Assert.True(shape.HitTest(new Point2D(60, 60)));     // inside interior (fills shape)
        Assert.True(shape.HitTest(new Point2D(110, 35)));    // near right edge
        Assert.False(shape.HitTest(new Point2D(-10, -10)));  // far outside
    }

    [Fact]
    public void HitTest_Polygon_DetectsClickOnEdge()
    {
        var shape = new PolygonShape(new[]
        {
            new Point2D(0, 0), new Point2D(100, 0),
            new Point2D(100, 100), new Point2D(0, 100)
        });

        Assert.True(shape.HitTest(new Point2D(50, 2)));     // near top edge
        Assert.True(shape.HitTest(new Point2D(50, 50)));    // inside
        Assert.False(shape.HitTest(new Point2D(200, 200))); // outside
    }

    [Fact]
    public void HitTest_Arc_DetectsClickOnArc()
    {
        var shape = new ArcShape(new Point2D(100, 100), 30, 0, 180);

        Assert.True(shape.HitTest(new Point2D(130, 100))); // at 0° (right)
        Assert.True(shape.HitTest(new Point2D(100, 130))); // at 90° (bottom in WPF coords)
        Assert.True(shape.HitTest(new Point2D(70, 100)));  // at 180° (left)
        Assert.False(shape.HitTest(new Point2D(100, 70))); // at 270° (top, outside arc)
    }

    [Fact]
    public void QuadTree_Rebuild_ReindexesSynchronizedWithSource()
    {
        var tree = CreateTree();
        var shapes = new List<Shape>
        {
            new CircleShape(new Point2D(50, 50), 10),
            new CircleShape(new Point2D(200, 200), 10)
        };

        tree.Rebuild(shapes);

        Assert.Contains(shapes[0], tree.Query(new Point2D(50, 50)));
        Assert.Contains(shapes[1], tree.Query(new Point2D(200, 200)));
    }
}
