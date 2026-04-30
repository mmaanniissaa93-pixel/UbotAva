using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntitySingleDespawnResponse : IPacketHandler
{
    public ushort Opcode => 0x3016;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        UBot.Protocol.ProtocolRuntime.SpawnController?.Despawn(packet.ReadUInt());
    }
}

