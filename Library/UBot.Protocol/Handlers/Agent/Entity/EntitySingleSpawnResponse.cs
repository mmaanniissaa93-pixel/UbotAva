using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntitySingleSpawnResponse : IPacketHandler
{
    public ushort Opcode => 0x3015;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        UBot.Protocol.ProtocolRuntime.SpawnController?.Parse(packet);
    }
}

