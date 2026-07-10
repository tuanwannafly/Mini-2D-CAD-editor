using CadEditor.Models;

namespace CadEditor.Commands;

public class TransformCommand : IEditorCommand
{
    private readonly Shape shape;
    private readonly double oldRotationDeg;
    private readonly double oldScaleX;
    private readonly double oldScaleY;
    private readonly double newRotationDeg;
    private readonly double newScaleX;
    private readonly double newScaleY;

    public TransformCommand(
        Shape shape,
        double oldRotationDeg, double oldScaleX, double oldScaleY,
        double newRotationDeg, double newScaleX, double newScaleY)
    {
        this.shape = shape;
        this.oldRotationDeg = oldRotationDeg;
        this.oldScaleX = oldScaleX;
        this.oldScaleY = oldScaleY;
        this.newRotationDeg = newRotationDeg;
        this.newScaleX = newScaleX;
        this.newScaleY = newScaleY;
    }

    public void Execute()
    {
        shape.RotationDeg = newRotationDeg;
        shape.ScaleX = newScaleX;
        shape.ScaleY = newScaleY;
    }

    public void Undo()
    {
        shape.RotationDeg = oldRotationDeg;
        shape.ScaleX = oldScaleX;
        shape.ScaleY = oldScaleY;
    }
}
