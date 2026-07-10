namespace CadEditor.Commands;

public class UndoRedoManager
{
    private readonly Stack<IEditorCommand> undoStack = new();
    private readonly Stack<IEditorCommand> redoStack = new();

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    public void ExecuteCommand(IEditorCommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear(); // có thao tác mới -> lịch sử redo cũ không còn hợp lệ
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = undoStack.Pop();
        command.Undo();
        redoStack.Push(command);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = redoStack.Pop();
        command.Execute();
        undoStack.Push(command);
    }
}