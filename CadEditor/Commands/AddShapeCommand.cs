using System.Collections.ObjectModel;
using CadEditor.Models;

namespace CadEditor.Commands;

public class AddShapeCommand : IEditorCommand
{
    private readonly ObservableCollection<Shape> shapes;
    private readonly Shape shape;

    public AddShapeCommand(ObservableCollection<Shape> shapes, Shape shape)
    {
        this.shapes = shapes;
        this.shape = shape;
    }

    public void Execute() => shapes.Add(shape);

    public void Undo() => shapes.Remove(shape);
}