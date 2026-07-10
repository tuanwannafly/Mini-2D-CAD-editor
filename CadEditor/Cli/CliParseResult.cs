namespace CadEditor.Cli;

public record CliParseResult(bool Success, CliCommand? Command, string? ErrorMessage)
{
    public static CliParseResult Ok(CliCommand command) => new(true, command, null);
    public static CliParseResult Fail(string error) => new(false, null, error);
}