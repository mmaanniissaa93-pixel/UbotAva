using UBot.Core.Network;

namespace UBot.Core.Abstractions.Network;

public interface IPacketDispatcher
{
    void Dispatch(object packet, PacketDestination destination);
}