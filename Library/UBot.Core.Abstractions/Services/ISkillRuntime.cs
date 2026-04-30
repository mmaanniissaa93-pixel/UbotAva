namespace UBot.Core.Abstractions.Services;

public interface ISkillRuntime
{
    void SendToServer(object packet, params object[] callbacks);
    object CreateActionAckCallback();
    object CreateBuffCastCallback(uint targetId, uint skillId);
    void AwaitCallback(object callback, int timeoutMilliseconds = 0);
    bool IsCallbackCompleted(object callback);
}
