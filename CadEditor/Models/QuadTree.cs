namespace CadEditor.Models;

public class QuadTree
{
    private readonly BoundingBox _bounds;
    private readonly int _capacity;
    private readonly int _maxDepth;
    private readonly int _depth;
    private List<Shape> _shapes = new();
    private QuadTree? _nw;
    private QuadTree? _ne;
    private QuadTree? _sw;
    private QuadTree? _se;
    private bool _divided;

    public QuadTree(BoundingBox bounds, int capacity = 4, int maxDepth = 8, int depth = 0)
    {
        _bounds = bounds;
        _capacity = capacity;
        _maxDepth = maxDepth;
        _depth = depth;
    }

    public BoundingBox Bounds => _bounds;

    public void Insert(Shape shape)
    {
        if (!_divided && _shapes.Count < _capacity || _depth >= _maxDepth)
        {
            _shapes.Add(shape);
            return;
        }

        if (!_divided)
            Subdivide();

        InsertIntoChildren(shape);
    }

    public void InsertAll(IEnumerable<Shape> shapes)
    {
        foreach (var s in shapes)
            Insert(s);
    }

    public List<Shape> Query(BoundingBox bounds)
    {
        var results = new List<Shape>();

        if (!_bounds.Overlaps(bounds))
            return results;

        foreach (var shape in _shapes)
        {
            if (bounds.Overlaps(shape.GetBounds()))
                results.Add(shape);
        }

        if (_divided)
        {
            results.AddRange(_nw!.Query(bounds));
            results.AddRange(_ne!.Query(bounds));
            results.AddRange(_sw!.Query(bounds));
            results.AddRange(_se!.Query(bounds));
        }

        return results;
    }

    public List<Shape> Query(Point2D point)
    {
        return Query(new BoundingBox(point.X, point.Y, point.X, point.Y));
    }

    public void Clear()
    {
        _shapes.Clear();
        _nw = _ne = _sw = _se = null;
        _divided = false;
    }

    public void Rebuild(IEnumerable<Shape> shapes)
    {
        Clear();
        InsertAll(shapes);
    }

    private void Subdivide()
    {
        double midX = _bounds.MinX + _bounds.Width / 2;
        double midY = _bounds.MinY + _bounds.Height / 2;

        _nw = new QuadTree(new BoundingBox(_bounds.MinX, _bounds.MinY, midX, midY), _capacity, _maxDepth, _depth + 1);
        _ne = new QuadTree(new BoundingBox(midX, _bounds.MinY, _bounds.MaxX, midY), _capacity, _maxDepth, _depth + 1);
        _sw = new QuadTree(new BoundingBox(_bounds.MinX, midY, midX, _bounds.MaxY), _capacity, _maxDepth, _depth + 1);
        _se = new QuadTree(new BoundingBox(midX, midY, _bounds.MaxX, _bounds.MaxY), _capacity, _maxDepth, _depth + 1);
        _divided = true;

        var retained = new List<Shape>();
        foreach (var shape in _shapes)
        {
            if (!InsertIntoChildren(shape))
                retained.Add(shape);
        }
        _shapes = retained;
    }

    private bool InsertIntoChildren(Shape shape)
    {
        var bounds = shape.GetBounds();
        bool placed = false;

        if (_nw!.Bounds.Overlaps(bounds))
        {
            _nw.Insert(shape);
            placed = true;
        }
        if (_ne!.Bounds.Overlaps(bounds))
        {
            _ne.Insert(shape);
            placed = true;
        }
        if (_sw!.Bounds.Overlaps(bounds))
        {
            _sw.Insert(shape);
            placed = true;
        }
        if (_se!.Bounds.Overlaps(bounds))
        {
            _se.Insert(shape);
            placed = true;
        }

        return placed;
    }
}
