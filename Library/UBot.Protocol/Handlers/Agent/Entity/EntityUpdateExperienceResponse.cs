using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdateExperienceResponse : IPacketHandler
{
    public ushort Opcode => 0x3056;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(EntityUpdateExperienceResponse), packet);
    }
}
