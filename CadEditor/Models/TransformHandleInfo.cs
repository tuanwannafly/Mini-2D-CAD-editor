namespace CadEditor.Models;

public enum HandleType
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    Rotation
}

public class TransformHandleInfo
{
    public double X { get; set; }
    public double Y { get; set; }
    public HandleType Type { get; set; }
}
