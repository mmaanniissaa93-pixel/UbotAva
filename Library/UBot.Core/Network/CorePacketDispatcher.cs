using UBot.Core.Abstractions.Network;

namespace UBot.Core.Network;

internal sealed class CorePacketDispatcher : IPacketDispatcher
{
    public void Dispatch(object packet, PacketDestination destination)
    {
        if (packet is Packet networkPacket)
            PacketManager.SendPacket(networkPacket, destination);
    }
}
