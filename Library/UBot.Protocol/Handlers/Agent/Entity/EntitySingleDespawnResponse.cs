using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Entity;

public class EntitySingleDespawnResponse : IPacketHandler
{
    public ushort Opcode => 0x3016;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.SpawnController?.Despawn(packet.ReadUInt());
    }
}
