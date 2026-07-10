using System.IO;
using CadEditor.Models;
using CadEditor.Services;

namespace CadEditor.Tests.Services;

public class JsonPersistenceServiceTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsPolymorphicShapes()
    {
        var service = new JsonPersistenceService();
        string path = Path.Combine(Path.GetTempPath(), $"cad-shapes-{Guid.NewGuid():N}.json");
        var shapes = new List<Shape>
        {
            new LineShape(new Point2D(1, 2), new Point2D(3, 4))
            {
                StrokeColor = "#FF0000",
                StrokeThickness = 1.5,
                RotationDeg = 15,
                ScaleX = 1.25,
                ScaleY = 0.75
            },
            new CircleShape(new Point2D(5, 6), 7),
            new RectangleShape(new Point2D(8, 9), 10, 11),
            new PolygonShape(new[] { new Point2D(12, 13), new Point2D(14, 15), new Point2D(16, 17) }),
            new ArcShape(new Point2D(18, 19), 20, 30, 120)
        };

        try
        {
            service.Save(path, shapes);

            List<Shape> loaded = service.Load(path);

            Assert.Equal(shapes.Count, loaded.Count);
            AssertLineShape(shapes[0], Assert.IsType<LineShape>(loaded[0]));
            AssertCircleShape(shapes[1], Assert.IsType<CircleShape>(loaded[1]));
            AssertRectangleShape(shapes[2], Assert.IsType<RectangleShape>(loaded[2]));
            AssertPolygonShape(shapes[3], Assert.IsType<PolygonShape>(loaded[3]));
            AssertArcShape(shapes[4], Assert.IsType<ArcShape>(loaded[4]));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void AssertShapeBase(Shape expected, Shape actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.IsSelected, actual.IsSelected);
        Assert.Equal(expected.StrokeColor, actual.StrokeColor);
        Assert.Equal(expected.StrokeThickness, actual.StrokeThickness);
        Assert.Equal(expected.RotationDeg, actual.RotationDeg);
        Assert.Equal(expected.ScaleX, actual.ScaleX);
        Assert.Equal(expected.ScaleY, actual.ScaleY);
    }

    private static void AssertLineShape(Shape expectedShape, LineShape actual)
    {
        var expected = Assert.IsType<LineShape>(expectedShape);
        AssertShapeBase(expected, actual);
        Assert.Equal(expected.Start, actual.Start);
        Assert.Equal(expected.End, actual.End);
    }

    private static void AssertCircleShape(Shape expectedShape, CircleShape actual)
    {
        var expected = Assert.IsType<CircleShape>(expectedShape);
        AssertShapeBase(expected, actual);
        Assert.Equal(expected.Center, actual.Center);
        Assert.Equal(expected.Radius, actual.Radius);
    }

    private static void AssertRectangleShape(Shape expectedShape, RectangleShape actual)
    {
        var expected = Assert.IsType<RectangleShape>(expectedShape);
        AssertShapeBase(expected, actual);
        Assert.Equal(expected.TopLeft, actual.TopLeft);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
    }

    private static void AssertPolygonShape(Shape expectedShape, PolygonShape actual)
    {
        var expected = Assert.IsType<PolygonShape>(expectedShape);
        AssertShapeBase(expected, actual);
        Assert.Equal(expected.Vertices, actual.Vertices);
    }

    private static void AssertArcShape(Shape expectedShape, ArcShape actual)
    {
        var expected = Assert.IsType<ArcShape>(expectedShape);
        AssertShapeBase(expected, actual);
        Assert.Equal(expected.Center, actual.Center);
        Assert.Equal(expected.Radius, actual.Radius);
        Assert.Equal(expected.StartAngleDeg, actual.StartAngleDeg);
        Assert.Equal(expected.EndAngleDeg, actual.EndAngleDeg);
    }
}
