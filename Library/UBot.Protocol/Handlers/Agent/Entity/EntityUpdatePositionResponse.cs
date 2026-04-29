using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdatePositionResponse : IPacketHandler
{
    public ushort Opcode => 0xB023;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityUpdatePositionResponse), packet);
    }
}
