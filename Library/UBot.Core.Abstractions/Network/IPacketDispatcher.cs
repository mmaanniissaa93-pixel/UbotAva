using UBot.Core.Network;

namespace UBot.Core.Abstractions.Network;

public interface IPacketDispatcher
{
    void RegisterHandler(object handler);
    void RemoveHandler(object handler);
    void RegisterHook(object hook);
    void RemoveHook(object hook);
    void SendPacket(object packet, PacketDestination destination, params object[] callbacks);
    void HandlePacket(object packet, PacketDestination destination);
    void Dispatch(object packet, PacketDestination destination);
}
