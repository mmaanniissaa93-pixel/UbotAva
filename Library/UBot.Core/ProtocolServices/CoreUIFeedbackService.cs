using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreUIFeedbackService : IUIFeedbackService
{
    public void Debug(string message) => Log.Debug(message);

    public void Notify(string message) => Log.Notify(message);

    public void Warn(string message) => Log.Warn(message);

    public void Status(string message) => Log.Status(message);
}

