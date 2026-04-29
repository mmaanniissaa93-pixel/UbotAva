using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreScriptProgress : IScriptProgress
{
    private readonly IUIFeedbackService _feedback;

    public CoreScriptProgress(IUIFeedbackService feedback)
    {
        _feedback = feedback;
    }

    public void Report(ScriptProgressUpdate update)
    {
        if (update == null)
            return;

        var line = update.LineIndex >= 0 ? $" line #{update.LineIndex}" : string.Empty;
        var message = $"[Script]{line} {update.Message}";

        switch (update.State)
        {
            case ScriptExecutionState.Failed:
                _feedback?.Warn(message);
                break;

            case ScriptExecutionState.Completed:
            case ScriptExecutionState.Loaded:
                _feedback?.Notify(message);
                break;

            default:
                _feedback?.Status(message);
                break;
        }
    }
}
