namespace CadEditor.Commands;

/// <summary>

/// </summary>
public interface IEditorCommand
{
    void Execute();
    void Undo();
}