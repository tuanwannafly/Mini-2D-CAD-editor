using System.Diagnostics;
using CadEditor.Models;

namespace CadEditor.Tests.Models;

public class QuadTreeBenchmarkTests
{
    private static readonly Random Rng = new(42);

    private static List<Shape> GenerateShapes(int count)
    {
        var shapes = new List<Shape>(count);
        for (int i = 0; i < count; i++)
        {
            var type = Rng.Next(4);
            double x = Rng.NextDouble() * 900 + 50;
            double y = Rng.NextDouble() * 900 + 50;
            Shape shape = type switch
            {
                0 => new LineShape(new Point2D(x, y), new Point2D(x + Rng.NextDouble() * 100, y + Rng.NextDouble() * 100)),
                1 => new CircleShape(new Point2D(x, y), Rng.NextDouble() * 40 + 5),
                2 => new RectangleShape(new Point2D(x, y), Rng.NextDouble() * 80 + 10, Rng.NextDouble() * 80 + 10),
                _ => new PolygonShape(new[]
                {
                    new Point2D(x, y),
                    new Point2D(x + Rng.NextDouble() * 60, y + Rng.NextDouble() * 60),
                    new Point2D(x + Rng.NextDouble() * 60, y)
                }),
            };
            shapes.Add(shape);
        }
        return shapes;
    }

    [Fact]
    public void Benchmark_NaiveVsQuadTree_With500Shapes()
    {
        var shapes = GenerateShapes(500);
        var clicks = new Point2D[100];
        for (int i = 0; i < clicks.Length; i++)
            clicks[i] = new Point2D(Rng.NextDouble() * 1000, Rng.NextDouble() * 1000);

        var tree = new QuadTree(new BoundingBox(0, 0, 1000, 1000), capacity: 8, maxDepth: 10);

        // Warm-up
        tree.Rebuild(shapes);
        var _ = tree.Query(clicks[0]);

        // --- QuadTree ---
        var swQ = Stopwatch.StartNew();
        int iterations = 50;
        for (int iter = 0; iter < iterations; iter++)
        {
            tree.Rebuild(shapes);
            foreach (var pt in clicks)
                tree.Query(pt);
        }
        swQ.Stop();
        double quadTreeMs = swQ.Elapsed.TotalMilliseconds / iterations;

        // --- Naive ---
        var swN = Stopwatch.StartNew();
        for (int iter = 0; iter < iterations; iter++)
        {
            foreach (var pt in clicks)
            {
                foreach (var shape in shapes)
                {
                    var bounds = shape.GetBounds();
                    if (bounds.Contains(pt.X, pt.Y))
                    {
                        shape.HitTest(pt);
                    }
                }
            }
        }
        swN.Stop();
        double naiveMs = swN.Elapsed.TotalMilliseconds / iterations;

        // Sanity: QuadTree should be faster or at least not pathologically slower
        // Assert that the result is in a reasonable range (not 0, not absurd)
        Assert.True(quadTreeMs > 0, "QuadTree benchmark took no time (measurement issue)");
        Assert.True(naiveMs > 0, "Naive benchmark took no time (measurement issue)");

        // Basic correctness: both should find the same topmost shape for each click
        for (int i = 0; i < 10; i++)
        {
            var pt = clicks[i];

            Shape? quadBest = null;
            int quadBestIdx = -1;
            var seen = new HashSet<Guid>();
            foreach (var c in tree.Query(pt))
            {
                if (!seen.Add(c.Id)) continue;
                if (!c.HitTest(pt)) continue;
                int idx = shapes.IndexOf(c);
                if (idx > quadBestIdx) { quadBestIdx = idx; quadBest = c; }
            }

            Shape? naiveBest = null;
            int naiveBestIdx = -1;
            for (int j = shapes.Count - 1; j >= 0; j--)
            {
                var s = shapes[j];
                if (s.HitTest(pt))
                {
                    if (j > naiveBestIdx) { naiveBestIdx = j; naiveBest = s; }
                }
            }

            Assert.Equal(naiveBest?.Id, quadBest?.Id);
        }

        // Output for CI/review (captured in test output)
        Console.WriteLine($"--- Benchmark (500 shapes, {iterations} iterations, {clicks.Length} queries/iter) ---");
        Console.WriteLine($"QuadTree: {quadTreeMs:F2} ms avg ({quadTreeMs / (clicks.Length * iterations) * 1000:F2} us/query)");
        Console.WriteLine($"Naive:    {naiveMs:F2} ms avg ({naiveMs / (clicks.Length * iterations) * 1000:F2} us/query)");
        Console.WriteLine($"Speedup:  {naiveMs / quadTreeMs:F1}x");
    }
}
