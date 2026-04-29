namespace UBot.Core.Abstractions.Services;

public sealed class ScriptProgressUpdate
{
    public ScriptProgressUpdate(
        ScriptExecutionState state,
        int lineIndex,
        string command,
        string message,
        object position = null
    )
    {
        State = state;
        LineIndex = lineIndex;
        Command = command;
        Message = message;
        Position = position;
    }

    public ScriptExecutionState State { get; }
    public int LineIndex { get; }
    public string Command { get; }
    public string Message { get; }
    public object Position { get; }
}
