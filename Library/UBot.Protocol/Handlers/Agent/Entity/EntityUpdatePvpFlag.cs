using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdatePvpFlag : IPacketHandler
{
    public ushort Opcode => 0xB516;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityUpdatePvpFlag), packet);
    }
}
