namespace UBot.Core.Abstractions.Services;

public interface IUIFeedbackService
{
    void Debug(string message);
    void Notify(string message);
    void Warn(string message);
    void Status(string message);
}
