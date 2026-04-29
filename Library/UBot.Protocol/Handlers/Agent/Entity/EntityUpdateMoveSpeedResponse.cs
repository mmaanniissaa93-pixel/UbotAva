using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdateMoveSpeedResponse : IPacketHandler
{
    public ushort Opcode => 0x30D0;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityUpdateMoveSpeedResponse), packet);
    }
}
