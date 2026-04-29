namespace UBot.Core.Abstractions.Services;

public interface IAlchemyRuntime
{
    object GetInventoryItemAt(byte slot);
    void SendToServer(object packet);
    void FireEvent(string eventName, params object[] args);
}
