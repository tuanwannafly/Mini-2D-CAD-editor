namespace CadEditor.Cli;

public abstract record CliCommand;

public record ExecuteEditorCliCommand(CadEditor.Commands.IEditorCommand EditorCommand) : CliCommand;
public record UndoCliCommand : CliCommand;
public record RedoCliCommand : CliCommand;
