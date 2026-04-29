using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreAlchemyProgress : IAlchemyProgress
{
    private readonly IUIFeedbackService _feedback;

    public CoreAlchemyProgress(IUIFeedbackService feedback)
    {
        _feedback = feedback;
    }

    public void Report(AlchemyProgressUpdate update)
    {
        if (update == null)
            return;

        var message = update.Percent > 0
            ? $"{update.Message} ({update.Percent}%)"
            : update.Message;

        switch (update.State)
        {
            case AlchemyOperationState.Error:
            case AlchemyOperationState.Destroyed:
            case AlchemyOperationState.TimedOut:
                _feedback?.Warn(message);
                break;

            case AlchemyOperationState.Succeeded:
                _feedback?.Notify(message);
                break;

            default:
                _feedback?.Status(message);
                break;
        }
    }
}
