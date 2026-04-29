namespace UBot.Core.Abstractions.Services;

public interface IProtocolLegacyHandler
{
    void Invoke(string handlerName, object packet);
    object ReplacePacket(string hookName, object packet);
}
