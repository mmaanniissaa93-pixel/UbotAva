using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdateStateResponse : IPacketHandler
{
    public ushort Opcode => 0x30BF;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityUpdateStateResponse), packet);
    }
}
