using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Entity;

public class EntitySingleSpawnResponse : IPacketHandler
{
    public ushort Opcode => 0x3015;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.SpawnController?.Parse(packet);
    }
}
