using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityRemoveOwnershipResponse : IPacketHandler
{
    public ushort Opcode => 0x304D;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityRemoveOwnershipResponse), packet);
    }
}
