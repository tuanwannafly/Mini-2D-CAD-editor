using CadEditor.Models;

namespace CadEditor.Commands;

public class MoveShapeCommand : IEditorCommand
{
    private readonly Shape shape;
    private readonly Point2D oldPosition;
    private readonly Point2D newPosition;

    public MoveShapeCommand(Shape shape, Point2D oldPosition, Point2D newPosition)
    {
        this.shape = shape;
        this.oldPosition = oldPosition;
        this.newPosition = newPosition;
    }

    public void Execute() => ShapeMover.MoveTo(shape, newPosition);

    public void Undo() => ShapeMover.MoveTo(shape, oldPosition);
}
