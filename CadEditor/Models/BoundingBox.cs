namespace CadEditor.Models;

public readonly record struct BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;

    public bool Overlaps(BoundingBox other) =>
        MinX <= other.MaxX && MaxX >= other.MinX &&
        MinY <= other.MaxY && MaxY >= other.MinY;

    public bool Contains(double x, double y) =>
        x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
}