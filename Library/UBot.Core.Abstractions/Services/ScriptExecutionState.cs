namespace UBot.Core.Abstractions.Services;

public enum ScriptExecutionState
{
    Loaded,
    Running,
    Paused,
    ExecutingCommand,
    CommandFinished,
    Movement,
    Completed,
    Failed,
    Stopped
}
