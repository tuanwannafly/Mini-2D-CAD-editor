using System.Collections.ObjectModel;
using CadEditor.Models;

namespace CadEditor.Commands;

public class DeleteShapeCommand : IEditorCommand
{
    private readonly ObservableCollection<Shape> shapes;
    private readonly Shape shape;
    private int index;

    public DeleteShapeCommand(ObservableCollection<Shape> shapes, Shape shape)
    {
        this.shapes = shapes;
        this.shape = shape;
    }

    public void Execute()
    {
        index = shapes.IndexOf(shape);
        shapes.Remove(shape);
    }

    public void Undo()
    {
        if (index < 0 || index > shapes.Count)
            shapes.Add(shape);
        else
            shapes.Insert(index, shape);
    }
}
