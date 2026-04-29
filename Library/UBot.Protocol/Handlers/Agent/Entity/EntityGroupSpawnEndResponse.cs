using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Entity;

public class EntityGroupSpawnEndResponse : IPacketHandler
{
    public ushort Opcode => 0x3018;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.SpawnController?.EndGroup();
    }
}
